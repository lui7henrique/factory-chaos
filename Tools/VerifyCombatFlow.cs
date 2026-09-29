using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Three actual projectiles, real physics, real damage and wave completion.</summary>
public static class VerifyCombatFlow
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Use a fresh SampleScene Play session.");
        bool oldBackground = Application.runInBackground;
        float oldScale = Time.timeScale;
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 0f;
            var cannon = Object.FindAnyObjectByType<CannonController>();
            var wave = Object.FindAnyObjectByType<WaveDirector>();
            if (wave.Current != WaveDirector.Phase.Preparing) throw new Exception("Fresh preparation required.");
            typeof(WaveDirector).GetMethod("Release", F).Invoke(wave, null);
            var muzzle = (Transform)typeof(CannonController).GetField("muzzle", F).GetValue(cannon);
            var pitchPivot = (Transform)typeof(CannonController).GetField("pitchPivot", F).GetValue(cannon);
            var fire = typeof(CannonController).GetMethod("Fire", F);
            while (!cannon.IsFull) cannon.TryAddRound();
            int shotCount = cannon.Rounds;
            Time.timeScale = 1f;
            for (int i = 0; i < shotCount; i++)
            {
                Vector3 direction = wave.Enemy.transform.position + Vector3.up * 0.85f - muzzle.position;
                pitchPivot.localRotation = Quaternion.Euler(-Mathf.Atan2(direction.y, direction.z) * Mathf.Rad2Deg, 0f, 0f);
                float healthBefore = wave.Enemy.Health;
                fire.Invoke(cannon, null);
                float end = Time.realtimeSinceStartup + 2f;
                while (wave.Enemy.Health >= healthBefore)
                {
                    if (Time.realtimeSinceStartup > end) throw new Exception("Actual projectile " + (i + 1) + " did not hit. Enemy HP: " + wave.Enemy.Health);
                    await Task.Delay(20);
                }
                await Task.Delay(300);
            }
            if (wave.Current != WaveDirector.Phase.Cleared || cannon.Rounds != 0 || !wave.Enemy.IsDead)
                throw new Exception("Three-hit combat loop did not finish correctly.");
            return new { shots = shotCount, rounds = cannon.Rounds, enemyHealth = wave.Enemy.Health,
                phase = wave.Current.ToString(), projectileCount = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude).Length };
        }
        finally { Application.runInBackground = oldBackground; Time.timeScale = oldScale; }
    }
}
