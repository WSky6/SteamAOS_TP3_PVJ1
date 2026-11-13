using UnityEditor;
using UnityEngine;

// Muestra solo los campos que usa cada tipo de enemigo
[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty typeProp = serializedObject.FindProperty("type");
        EditorGUILayout.PropertyField(typeProp, new GUIContent("Tipo de enemigo"));

        EnemyData.EnemyType t = (EnemyData.EnemyType)typeProp.enumValueIndex;

        switch (t)
        {
            case EnemyData.EnemyType.Melee:
                Group("Vida", "maxHealth");
                Group("Movimiento", "moveSpeed", "acceleration", "angularSpeed");
                Group("Salto", "maxJumpHeight", "jumpDistance", "jumpTriggerDistance",
                               "jumpSpeed", "jumpArcHeight", "jumpCooldown");
                Group("Percepción", "visionRange", "visionAngle");
                Group("Mirar a los lados", "headScanAngle", "headScanSpeed");
                Group("Alerta a aliados", "allyAlertRadius");
                Group("Ataque cuerpo a cuerpo", "attackRange", "attackAngle", "instantKill",
                                                "attackDamage", "attackWindup", "attackCooldown");
                Group("Recompensa", "points");
                break;

            case EnemyData.EnemyType.Distancia:
                Group("Vida", "maxHealth");
                Group("Percepción", "visionRange", "visionAngle", "turnSpeed");
                Group("Mirar a los lados", "headScanAngle", "headScanSpeed");
                Group("Alerta", "alertMemoryTime");
                Group("Disparo", "bulletPrefab", "bulletSpeed", "bulletDamage", "shootInterval");
                Group("Recompensa", "points");
                break;

            case EnemyData.EnemyType.Boss:
                Group("Vida", "hitsToKill");
                Group("Movimiento", "moveSpeed", "acceleration", "angularSpeed");
                Group("Salto", "maxJumpHeight", "jumpDistance", "jumpTriggerDistance",
                               "jumpSpeed", "jumpArcHeight", "jumpCooldown");
                Group("Percepción", "visionRange");
                Group("Ataque cuerpo a cuerpo", "attackRange", "attackAngle", "instantKill",
                                                "attackDamage", "attackWindup", "attackCooldown");
                Group("Disparo", "bulletPrefab", "bulletSpeed", "bulletDamage");
                Group("Ráfaga", "burstShots", "burstShotDelay", "burstInterval", "shootMinDistance");
                Group("Recompensa", "points");
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void Group(string title, params string[] fields)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (string f in fields)
        {
            SerializedProperty p = serializedObject.FindProperty(f);
            if (p != null) EditorGUILayout.PropertyField(p, true);
        }
    }
}