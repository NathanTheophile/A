using Com.IsartDigital.F2P.Player.PlayerProfiles;
using UnityEditor;
using UnityEngine;

//Author: Clément DUCROQUET

namespace Com.IsartDigital.Editors
{
    [CustomEditor(typeof(PlayerAchitecture))]
    public class PlayerCustomEditor : Editor
    {
        #region Editor Settings

        private const uint TITLE_SPACING = 7;
        private const uint ITEM_SPACING = 1;
        private const uint SECTION_SPACING = 25;

        private const string SPEED_HEADER = "Speed";
        private const string ROTATION_HEADER = "Rotation";

        #endregion

        #region Object Properties

        private const string MOVING_SPEED_PROPERTY_NAME = "_MovingSpeed";
        private const string TIME_TO_FULL_SPEED_PROPERTY_NAME = "_TimeToFullSpeed";
        private const string TIME_TO_DECELERATE_PROPERTY_NAME = "_TimeToDecelerate";
        private const string ROTATION_SPEED_PROPERTY_NAME = "_RotationSpeed";
        private const string SHIELD_SIZE_PROPERTY_NAME = "_ShieldSize";
        private const string SHIELD_SHAPE_PROPERTY_NAME = "_ShieldShape";
        private const string POWER_PROPERTY_NAME = "_Power";

        #endregion

        #region Tab Management

        private const string TAB_A_NAME = "Character";
        private const string TAB_B_NAME = "Equipment";
        private const string TAB_C_NAME = "Power Settings";

        private int currentTabIndex;
        private string[] tabs = new string[] { TAB_A_NAME, TAB_B_NAME, TAB_C_NAME };

        #endregion

        public override void OnInspectorGUI()
        {
            //To work with the last version of the serializedObject.
            serializedObject.Update();

            //Tab Management
            currentTabIndex = GUILayout.Toolbar(currentTabIndex, tabs);

            switch(currentTabIndex)
            {
                case 0:
                    //Title
                    EditorGUILayout.LabelField(SPEED_HEADER, EditorStyles.boldLabel);
                    EditorGUILayout.Space(TITLE_SPACING);

                    //Fields
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(MOVING_SPEED_PROPERTY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(TIME_TO_FULL_SPEED_PROPERTY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(TIME_TO_DECELERATE_PROPERTY_NAME));
                    EditorGUILayout.Space(SECTION_SPACING);

                    //Title
                    EditorGUILayout.LabelField(ROTATION_HEADER, EditorStyles.boldLabel);
                    EditorGUILayout.Space(TITLE_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(ROTATION_SPEED_PROPERTY_NAME));
                    break;
                
                case 1:
                    //Fields
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SHIELD_SIZE_PROPERTY_NAME));
                    EditorGUILayout.Space(ITEM_SPACING);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(SHIELD_SHAPE_PROPERTY_NAME));
                    break;
                
                case 2:
                    //Fields
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(POWER_PROPERTY_NAME));
                    break;
            }

            //Save
            serializedObject.ApplyModifiedProperties();
        }
    }
}