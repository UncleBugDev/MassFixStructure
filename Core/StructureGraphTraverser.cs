using System;
using System.Collections.Generic;
using HarmonyLib;
using MassFixStructure.Configuration;
using UnityEngine;

namespace MassFixStructure.Core
{
    public static class StructureGraphTraverser
    {
        // Reusable pooled collections to avoid runtime GC allocations
        private static readonly Queue<WearNTear> s_queue = new Queue<WearNTear>(512);
        private static readonly HashSet<WearNTear> s_visited = new HashSet<WearNTear>(1024);
        private static readonly Collider[] s_overlapBuffer = new Collider[1024];

        private static int s_pieceMask;
        private static int PieceMask
        {
            get
            {
                if (s_pieceMask == 0)
                {
                    s_pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
                    if (s_pieceMask == 0)
                    {
                        s_pieceMask = LayerMask.GetMask("piece");
                    }
                    if (s_pieceMask == 0)
                    {
                        int p = LayerMask.NameToLayer("piece");
                        if (p >= 0) s_pieceMask |= (1 << p);
                        int pns = LayerMask.NameToLayer("piece_nonsolid");
                        if (pns >= 0) s_pieceMask |= (1 << pns);
                    }
                }
                return s_pieceMask;
            }
        }
        private static readonly Vector3 s_contactMargin = new Vector3(0.18f, 0.18f, 0.18f);

        // Fast cached delegate for private Player.CheckCanRemovePiece(Piece)
        private static readonly Func<Player, Piece, bool>? s_checkCanRemovePieceDelegate;

        static StructureGraphTraverser()
        {
            try
            {
                var method = AccessTools.Method(typeof(Player), "CheckCanRemovePiece", new[] { typeof(Piece) });
                if (method != null)
                {
                    s_checkCanRemovePieceDelegate = AccessTools.MethodDelegate<Func<Player, Piece, bool>>(method);
                }
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogWarning($"Could not bind CheckCanRemovePiece delegate: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds all eligible damaged pieces connected to or near the origin piece based on configured mode.
        /// </summary>
        public static List<WearNTear> FindPiecesToRepair(
            Player player,
            WearNTear originWnt,
            RepairMode mode,
            float maxRadius,
            int maxRepairs,
            int maxVisitedNodes,
            bool requireWorkbench)
        {
            var results = new List<WearNTear>(Math.Min(maxRepairs, 256));

            if (originWnt == null || player == null)
            {
                return results;
            }

            s_queue.Clear();
            s_visited.Clear();

            Vector3 originPos = originWnt.transform.position;
            Piece? originPiece = originWnt.GetComponent<Piece>();
            Piece.PieceCategory? targetCategory = originPiece != null ? originPiece.m_category : (Piece.PieceCategory?)null;

            if (mode == RepairMode.Radius)
            {
                FindPiecesInRadius(player, originPos, maxRadius, maxRepairs, requireWorkbench, results);
            }
            else
            {
                bool sameCategoryOnly = (mode == RepairMode.ConnectedSameCategory) && targetCategory.HasValue;
                FindPiecesConnected(player, originWnt, originPos, maxRadius, maxRepairs, maxVisitedNodes, requireWorkbench, sameCategoryOnly, targetCategory, results);
            }

            return results;
        }

        private static void FindPiecesInRadius(
            Player player,
            Vector3 originPos,
            float radius,
            int maxRepairs,
            bool requireWorkbench,
            List<WearNTear> results)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(originPos, radius, s_overlapBuffer, PieceMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = s_overlapBuffer[i];
                if (hit == null || hit.isTrigger || hit.attachedRigidbody != null)
                {
                    continue;
                }

                WearNTear? wnt = hit.GetComponentInParent<WearNTear>();
                if (wnt == null || !s_visited.Add(wnt))
                {
                    continue;
                }

                if (NeedsRepair(wnt) && HasRepairPermission(player, wnt, requireWorkbench))
                {
                    results.Add(wnt);
                    if (results.Count >= maxRepairs)
                    {
                        break;
                    }
                }
            }
        }

        private static void FindPiecesConnected(
            Player player,
            WearNTear originWnt,
            Vector3 originPos,
            float maxRadius,
            int maxRepairs,
            int maxVisitedNodes,
            bool requireWorkbench,
            bool sameCategoryOnly,
            Piece.PieceCategory? targetCategory,
            List<WearNTear> results)
        {
            s_visited.Add(originWnt);
            s_queue.Enqueue(originWnt);

            // Check if origin piece itself needs repair
            if (NeedsRepair(originWnt) && HasRepairPermission(player, originWnt, requireWorkbench))
            {
                results.Add(originWnt);
            }

            while (s_queue.Count > 0 && s_visited.Count < maxVisitedNodes && results.Count < maxRepairs)
            {
                WearNTear current = s_queue.Dequeue();
                if (current == null)
                {
                    continue;
                }

                Collider[] colliders = current.GetComponentsInChildren<Collider>(false);
                if (colliders == null || colliders.Length == 0)
                {
                    continue;
                }

                for (int cIdx = 0; cIdx < colliders.Length; cIdx++)
                {
                    Collider col = colliders[cIdx];
                    if (col == null || col.isTrigger || col.attachedRigidbody != null)
                    {
                        continue;
                    }

                    Vector3 center;
                    Vector3 halfExtents;
                    Quaternion rotation;

                    if (col is BoxCollider box)
                    {
                        Transform t = box.transform;
                        center = t.TransformPoint(box.center);
                        halfExtents = Vector3.Scale(box.size * 0.5f, t.lossyScale) + s_contactMargin;
                        rotation = t.rotation;
                    }
                    else
                    {
                        Bounds b = col.bounds;
                        center = b.center;
                        halfExtents = b.extents + s_contactMargin;
                        rotation = Quaternion.identity;
                    }

                    int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, s_overlapBuffer, rotation, PieceMask);
                    for (int hIdx = 0; hIdx < hitCount; hIdx++)
                    {
                        Collider hit = s_overlapBuffer[hIdx];
                        if (hit == null || hit.isTrigger || hit.attachedRigidbody != null)
                        {
                            continue;
                        }

                        WearNTear? neighbor = hit.GetComponentInParent<WearNTear>();
                        if (neighbor == null || !s_visited.Add(neighbor))
                        {
                            continue;
                        }

                        // Distance guardrail
                        if (Vector3.Distance(originPos, neighbor.transform.position) > maxRadius)
                        {
                            continue;
                        }

                        // Category guardrail
                        if (sameCategoryOnly)
                        {
                            Piece? piece = neighbor.GetComponent<Piece>();
                            if (piece == null || piece.m_category != targetCategory!.Value)
                            {
                                continue;
                            }
                        }

                        // Enqueue to continue traversing connected structure
                        s_queue.Enqueue(neighbor);

                        // If damaged and player has rights, add to repair list
                        if (NeedsRepair(neighbor) && HasRepairPermission(player, neighbor, requireWorkbench))
                        {
                            results.Add(neighbor);
                            if (results.Count >= maxRepairs)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        public static bool NeedsRepair(WearNTear wnt)
        {
            if (wnt == null)
            {
                return false;
            }

            ZNetView nview = wnt.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            ZDO zdo = nview.GetZDO();
            if (zdo == null)
            {
                return false;
            }

            float currentHealth = zdo.GetFloat(ZDOVars.s_health, wnt.m_health);
            return currentHealth < (wnt.m_health - 0.01f);
        }

        public static bool HasRepairPermission(Player player, WearNTear wnt, bool requireWorkbench)
        {
            if (wnt == null || player == null)
            {
                return false;
            }

            // Ward access check (prevent griefing in private areas)
            if (!PrivateArea.CheckAccess(wnt.transform.position, 0f, flash: false))
            {
                return false;
            }

            // Workbench check
            if (requireWorkbench)
            {
                Piece? piece = wnt.GetComponent<Piece>();
                if (piece != null)
                {
                    if (s_checkCanRemovePieceDelegate != null)
                    {
                        if (!s_checkCanRemovePieceDelegate(player, piece))
                        {
                            return false;
                        }
                    }
                    else if (piece.m_craftingStation != null)
                    {
                        if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position) &&
                            !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
