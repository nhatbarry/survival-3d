using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Fires one or more hitscan beams in a fan and draws them, with optional piercing.
    /// Shared by PlayerShooting and PlayerShootingMobile.
    /// </summary>
    public class GunBeams
    {
        public float spreadAngle = 10f;                     // Degrees between neighbouring beams.


        readonly Transform origin;                          // The end of the gun barrel.
        readonly LineRenderer mainLine;                     // The gun's own line renderer, used for the first beam.
        readonly int shootableMask;
        readonly List<LineRenderer> lines = new List<LineRenderer> ();
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly List<EnemyHealth> damagedEnemies = new List<EnemyHealth> ();


        public GunBeams (Transform origin, LineRenderer mainLine, int shootableMask)
        {
            this.origin = origin;
            this.mainLine = mainLine;
            this.shootableMask = shootableMask;
            lines.Add (mainLine);
        }


        public void Fire (float range, int damage, int beamCount, int pierceCount)
        {
            beamCount = Mathf.Max (1, beamCount);
            EnsureLines (beamCount);

            for(int i = 0; i < beamCount; i++)
            {
                // Spread the beams evenly around the forward direction.
                float angle = (i - (beamCount - 1) * 0.5f) * spreadAngle;
                Vector3 direction = Quaternion.AngleAxis (angle, Vector3.up) * origin.forward;

                Vector3 end = FireBeam (origin.position, direction, range, damage, pierceCount);

                lines[i].enabled = true;
                lines[i].SetPosition (0, origin.position);
                lines[i].SetPosition (1, end);
            }
        }


        public void Disable ()
        {
            foreach(LineRenderer line in lines)
            {
                line.enabled = false;
            }
        }


        // Damages up to pierceCount + 1 enemies along the ray and returns where the beam stops.
        Vector3 FireBeam (Vector3 start, Vector3 direction, float range, int damage, int pierceCount)
        {
            int count = Physics.RaycastNonAlloc (new Ray (start, direction), hits, range, shootableMask);
            System.Array.Sort (hits, 0, count, HitDistanceComparer.Instance);

            damagedEnemies.Clear ();
            int enemiesLeft = pierceCount + 1;

            for(int i = 0; i < count; i++)
            {
                EnemyHealth enemy = hits[i].collider.GetComponent <EnemyHealth> ();

                // Anything that isn't an enemy (walls, props) stops the beam.
                if(enemy == null)
                    return hits[i].point;

                // Skip dead enemies and enemies already hit through another of their colliders.
                if(enemy.currentHealth <= 0 || damagedEnemies.Contains (enemy))
                    continue;

                enemy.TakeDamage (damage, hits[i].point);
                damagedEnemies.Add (enemy);

                if(--enemiesLeft == 0)
                    return hits[i].point;
            }

            return start + direction * range;
        }


        void EnsureLines (int count)
        {
            while(lines.Count < count)
            {
                GameObject go = new GameObject ("ExtraBeam" + lines.Count);
                go.transform.SetParent (origin, false);

                LineRenderer line = go.AddComponent <LineRenderer> ();
                line.sharedMaterials = mainLine.sharedMaterials;
                line.widthCurve = mainLine.widthCurve;
                line.widthMultiplier = mainLine.widthMultiplier;
                line.colorGradient = mainLine.colorGradient;
                line.textureMode = mainLine.textureMode;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;

                lines.Add (line);
            }
        }


        class HitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer ();

            public int Compare (RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo (b.distance);
            }
        }
    }
}
