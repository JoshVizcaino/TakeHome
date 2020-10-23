using UnityEditor;
using ControlMode = ScrollPositionCtrl.ControlMode;

[CustomEditor(typeof(ScrollPositionCtrl))]
[CanEditMultipleObjects]
public class ScrollPositionCtrlEditor : Editor
{
	private SerializedProperty GetProperty(string proptyName)
	{
		return serializedObject.FindProperty(proptyName);
	}

	private void SetPropertyField(string proptyName, bool includeChildren = false)
	{
		EditorGUILayout.PropertyField(
			serializedObject.FindProperty(proptyName), includeChildren);
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		// Basic configuration
		SetPropertyField("scrollType");
		SetPropertyField("controlMode");
		if (GetProperty("controlMode").enumValueIndex == (int) ControlMode.Drag) {
			++EditorGUI.indentLevel;
			SetPropertyField("alignMiddle");
			--EditorGUI.indentLevel;
		}
		SetPropertyField("direction");
		SetPropertyField("mWidgets", true);
		SetPropertyField("scrollBank");
		SetPropertyField("centeredContentID");

		// Appearance 
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Scroll Appearance", EditorStyles.boldLabel);
		SetPropertyField("widgetDensity");
		SetPropertyField("widgetPositionCurve");
		SetPropertyField("widgetScaleCurve");
		SetPropertyField("widgetMovementCurve");

		// Events
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Widget Event", EditorStyles.boldLabel);
		SetPropertyField("onWidgetClick");

		serializedObject.ApplyModifiedProperties();
	}
}
