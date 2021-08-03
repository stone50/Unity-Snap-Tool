using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

static class Details
{
    public static bool preview = true;
    public static EditorWindow window;
    public static Vector3 center;
    public static Vector3 extents;
    public static Vector3 direction;
    public static Vector3 absDirection;
    public static Vector3 distance;

    //multiplies vector components
    public static Vector3 multiplyVectors(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(GameObject))]
public class Draw : Editor
{

    //draws the bouding box, direction arrow, and destination box
    void OnSceneGUI()
    {
        if (Details.window != null && Details.preview)
        {

            //bounding box
            Handles.color = Color.magenta;
            Handles.DrawWireCube(Details.center, Details.extents * 2);

            //direction arrow
            if (Details.distance.magnitude > Details.multiplyVectors(Details.extents * 2, Details.absDirection).magnitude + (Details.extents.magnitude / 2))
            {
                Handles.ArrowHandleCap(
                    0,
                    Details.center + Details.multiplyVectors(Details.extents, Details.direction),
                    Quaternion.LookRotation(Details.direction),
                    Details.extents.magnitude / 2,
                    EventType.Repaint
                );
            }

            //destination box
            if (Details.distance.magnitude != 0)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(Details.center + Details.distance, Details.extents * 2);
            }
        }
    }
}

public class snapObjects : EditorWindow
{
    static Transform[] lastTrans;

    static int snapDirectionIndex;
    static Vector2 scrollPos;

    //window creation
    [MenuItem("CONTEXT/Transform/Snap")]
    public static void ShowWindow(MenuCommand command)
    {
        if (Details.window)
        {
            Details.window.Close();
        }
        if (Selection.gameObjects.Length == 1)
        {
            Details.window = CreateWindow<snapObjects>("Snap " + Selection.gameObjects[0].name);
        }
        else
        {
            Details.window = CreateWindow<snapObjects>("Snap Selection");
        }

        Details.direction = new Vector3(1, 0, 0);
        Details.absDirection = new Vector3(1, 0, 0);
        if (Details.preview)
        {
            findCenterAndExtents();
            calcDist();
        }

        Selection.selectionChanged += selectionHasChanged;
        lastTrans = Selection.transforms;

        snapDirectionIndex = 0;
        scrollPos = new Vector2(0, 0);
    }

    //window elements
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space();

        //snap direction dropdown
        EditorGUI.LabelField(GUILayoutUtility.GetRect(0, 20), "Direction");
        int choiceIndex = EditorGUILayout.Popup(snapDirectionIndex, new string[] { "Positive X", "Negative X", "Positive Y", "Negative Y", "Positive Z", "Negtaive Z" });
        if (choiceIndex != snapDirectionIndex)
        {
            snapDirectionIndex = choiceIndex;

            switch (snapDirectionIndex)
            {
                case 0:
                    Details.direction = new Vector3(1, 0, 0);
                    Details.absDirection = new Vector3(1, 0, 0);
                    break;

                case 1:
                    Details.direction = new Vector3(-1, 0, 0);
                    Details.absDirection = new Vector3(1, 0, 0);
                    break;

                case 2:
                    Details.direction = new Vector3(0, 1, 0);
                    Details.absDirection = new Vector3(0, 1, 0);
                    break;

                case 3:
                    Details.direction = new Vector3(0, -1, 0);
                    Details.absDirection = new Vector3(0, 1, 0);
                    break;

                case 4:
                    Details.direction = new Vector3(0, 0, 1);
                    Details.absDirection = new Vector3(0, 0, 1);
                    break;

                case 5:
                    Details.direction = new Vector3(0, 0, -1);
                    Details.absDirection = new Vector3(0, 0, 1);
                    break;

                default:
                    Details.window.Close();
                    Debug.LogError("Unrecognized Direction");
                    break;
            }
        }

        EditorGUILayout.Space();

        Details.preview = GUI.Toggle(GUILayoutUtility.GetRect(0, 20), Details.preview, "Preview");

        EditorGUILayout.Space();

        //snap button
        if (GUI.Button(GUILayoutUtility.GetRect(0, 20), "Snap"))
        {
            snap();
        }

        EditorGUILayout.EndScrollView();

        if (Details.preview)
        {

            calcDist();
            EditorWindow.GetWindow<SceneView>().Repaint();
        }
    }

    //closes window if selection changes
    public static void selectionHasChanged()
    {
        Details.window.Close();
        Selection.selectionChanged -= selectionHasChanged;
    }

    //performs boxcast and calculated predicted position
    public static void calcDist()
    {
        RaycastHit[] hits = Physics.BoxCastAll(Details.center, Details.extents, Details.direction);

        //finds closest hit
        RaycastHit closestHit = new RaycastHit();
        float closestDist = -1;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance != 0)
            {
                if (closestDist == -1 || hit.distance < closestDist)
                {
                    closestHit = hit;
                    closestDist = hit.distance;
                }
            }
        }

        if (closestDist == -1)
        {
            Details.distance = new Vector3(0, 0, 0);
            return;
        }

        Details.distance = Details.multiplyVectors(closestHit.point, Details.absDirection) - (Details.multiplyVectors(Details.center, Details.absDirection) + Details.multiplyVectors(Details.extents, Details.direction));
    }

    //updates position and drawings
    void Update()
    {
        if (Selection.transforms != lastTrans)
        {
            lastTrans = Selection.transforms;
            if (Details.preview)
            {
                findCenterAndExtents();
                calcDist();
            }
        }
    }

    //snaps the selection into place
    public static void snap()
    {
        if (!Details.preview)
        {
            findCenterAndExtents();
            calcDist();
        }
        foreach (Transform trans in Selection.transforms)
        {
            trans.position += Details.distance;
        }
        Details.window.Close();
    }

    //finds the center and extents of the meshes
    public static void findCenterAndExtents()
    {

        Collider collider = Selection.gameObjects[0].GetComponentInChildren<Collider>();
        if (collider == null)
        {
            Details.window.Close();
            Debug.LogError("No Colliders Found");
            return;
        }

        float maxX = collider.bounds.center.x + collider.bounds.extents.x;
        float maxY = collider.bounds.center.y + collider.bounds.extents.y;
        float maxZ = collider.bounds.center.z + collider.bounds.extents.z;
        float minX = collider.bounds.center.x - collider.bounds.extents.x;
        float minY = collider.bounds.center.y - collider.bounds.extents.y;
        float minZ = collider.bounds.center.z - collider.bounds.extents.z;

        foreach (GameObject obj in Selection.gameObjects)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider coll in colliders)
            {
                maxX = Mathf.Max(maxX, coll.bounds.center.x + coll.bounds.extents.x);
                maxY = Mathf.Max(maxY, coll.bounds.center.y + coll.bounds.extents.y);
                maxZ = Mathf.Max(maxZ, coll.bounds.center.z + coll.bounds.extents.z);
                minX = Mathf.Min(minX, coll.bounds.center.x - coll.bounds.extents.x);
                minY = Mathf.Min(minY, coll.bounds.center.y - coll.bounds.extents.y);
                minZ = Mathf.Min(minZ, coll.bounds.center.z - coll.bounds.extents.z);
            }
        }

        Details.center = new Vector3((maxX + minX) / 2, (maxY + minY) / 2, (maxZ + minZ) / 2);
        Details.extents = new Vector3(maxX - Details.center.x, maxY - Details.center.y, maxZ - Details.center.z);
    }
}