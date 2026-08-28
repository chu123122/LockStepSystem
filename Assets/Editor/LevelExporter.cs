using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Client.Unit;
using LockStepCore.Level;
using LockStepCore.Physics;

namespace Client.Level
{
    public static class LevelExporter
    {
        [MenuItem("Tools/Auto Assign And Export World")]
        public static void AutoAssignAndExport()
        {
            var balls = Object.FindObjectsOfType<BallController>()
                .OrderBy(b => b.transform.GetSiblingIndex())
                .ToArray();

            for (var i = 0; i < balls.Length; i++)
            {
                var auth = balls[i].GetComponent<WorldEntityAuthoring>();
                if (auth == null)
                    auth = balls[i].gameObject.AddComponent<WorldEntityAuthoring>();
                auth.entityId = i;
                auth.shape = Shape.Circle;
                auth.size = 0.5f;
                auth.isDynamic = true;
            }

            Debug.Log($"已为 {balls.Length} 个球挂上 WorldEntityAuthoring");
            Export();
        }

        [MenuItem("Tools/Export World Data")]
        public static void Export()
        {
            var spawns = new List<EntitySpawn>();
            var authorings = Object.FindObjectsOfType<WorldEntityAuthoring>();
            foreach (var auth in authorings)
            {
                var pos = auth.transform.position;
                spawns.Add(new EntitySpawn
                {
                    EntityId = auth.entityId,
                    Shape = auth.shape,
                    X = pos.x,
                    Y = pos.y,
                    Z = pos.z,
                    Size = auth.size,
                    IsDynamic = auth.isDynamic,
                });
            }

            spawns.Sort((a, b) => a.EntityId.CompareTo(b.EntityId));

            var data = new LevelData { Spawns = spawns };
            var json = JsonUtility.ToJson(data, true);

            var dir = Path.Combine(Application.dataPath, "../Data/levels");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "world.json");
            File.WriteAllText(path, json);

            Debug.Log($"导出完成:{spawns.Count} 个实体 → {path}");
        }
    }
}
