using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Vertex Animation Texture (VAT) Baker
/// - SkinnedMeshRenderer + AnimationClip ¡æ Position Texture + Normal Texture
/// - RGBAHalf Æ÷¸ËÀ¸·Î ÀúÀå (URP ÃÖÀûÈ­)
/// </summary>
public class VATBaker : EditorWindow
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Inspector Fields
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private GameObject _targetObject;
    private AnimationClip _clip;
    private int _fps = 30;
    private bool _bakeNormals = true;
    private string _savePath = "Assets/VAT";

    // ³»ºÎ »óÅÂ
    private string _statusMessage = "";
    private bool _isBaking = false;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Menu Item
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [MenuItem("Tools/VAT Baker")]
    public static void OpenWindow()
    {
        var window = GetWindow<VATBaker>("VAT Baker");
        window.minSize = new Vector2(360, 320);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // GUI
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OnGUI()
    {
        GUILayout.Label("Vertex Animation Texture Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object", _targetObject, typeof(GameObject), true);

        _clip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", _clip, typeof(AnimationClip), false);

        _fps = EditorGUILayout.IntSlider("FPS", _fps, 1, 60);
        _bakeNormals = EditorGUILayout.Toggle("Bake Normals", _bakeNormals);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Save Path");
        _savePath = EditorGUILayout.TextField(_savePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Àý´ë °æ·Î ¡æ »ó´ë °æ·Î
                if (selected.StartsWith(Application.dataPath))
                    _savePath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // À¯È¿¼º °Ë»ç
        bool canBake = _targetObject != null && _clip != null && !_isBaking;
        if (!canBake && !_isBaking)
        {
            EditorGUILayout.HelpBox("Target Object¿Í Animation ClipÀ» ÁöÁ¤ÇØÁÖ¼¼¿ä.", MessageType.Info);
        }

        GUI.enabled = canBake;
        if (GUILayout.Button(_isBaking ? "Baking..." : "Bake VAT", GUILayout.Height(36)))
        {
            Bake();
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Bake ¸ÞÀÎ ·ÎÁ÷
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Bake()
    {
        _isBaking = true;
        _statusMessage = "º£ÀÌÅ· ÁØºñ Áß...";
        Repaint();

        try
        {
            // 1. SkinnedMeshRenderer È¹µæ
            var smr = _targetObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null)
            {
                _statusMessage = "? SkinnedMeshRenderer¸¦ Ã£À» ¼ö ¾ø½À´Ï´Ù.";
                return;
            }

            // 2. ÇÁ·¹ÀÓ ¼ö °è»ê
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(_clip.length * _fps));
            float deltaTime = _clip.length / totalFrames;
            int vertCount = smr.sharedMesh.vertexCount;

            _statusMessage = $"¹öÅØ½º {vertCount}°³ ¡¿ {totalFrames}ÇÁ·¹ÀÓ º£ÀÌÅ· Áß...";
            Repaint();

            // 3. ÅØ½ºÃ³ Å©±â °áÁ¤ (¹öÅØ½º ¼ö¸¦ ³Êºñ, ÇÁ·¹ÀÓÀ» ³ôÀÌ)
            //    ¡Ø GPU ÃÖ´ë ÅØ½ºÃ³ Å©±â 16384 ÃÊ°ú ½Ã °æ°í
            if (vertCount > 16384 || totalFrames > 16384)
            {
                _statusMessage = $"? ÅØ½ºÃ³ Å©±â ÃÊ°ú (¹öÅØ½º:{vertCount}, ÇÁ·¹ÀÓ:{totalFrames}). ¸Þ½Ã ¶Ç´Â FPS/±æÀÌ¸¦ ÁÙ¿©ÁÖ¼¼¿ä.";
                return;
            }

            // 4. SampleAnimation ¹æ½Ä - rootGO È¹µæ
            var rootGO = smr.transform.root.gameObject;

            // 5. ¸ðµç ÇÁ·¹ÀÓ ¼øÈ¸ ¡æ ¿ÀÇÁ¼Â ¼öÁý + bounds °è»ê
            var allOffsets = new Vector3[totalFrames * vertCount]; // Àý´ë À§Ä¡
            var allNormals = new Vector3[totalFrames * vertCount];
            float posMin = float.MaxValue, posMax = float.MinValue;

            var bakedMesh = new Mesh();

            for (int f = 0; f < totalFrames; f++)
            {
                float t = f * deltaTime;
                SampleFrame(smr, rootGO, _clip, t, bakedMesh);

                Vector3[] verts = bakedMesh.vertices;
                Vector3[] normals = bakedMesh.normals;

                for (int v = 0; v < vertCount; v++)
                {
                    // Àý´ë À§Ä¡ ÀúÀå (¿ÀÇÁ¼Â ¾Æ´Ô)
                    Vector3 pos = verts[v];
                    allOffsets[f * vertCount + v] = pos;

                    posMin = Mathf.Min(posMin, pos.x, pos.y, pos.z);
                    posMax = Mathf.Max(posMax, pos.x, pos.y, pos.z);

                    if (_bakeNormals)
                        allNormals[f * vertCount + v] = normals[v];
                }
            }

            // bounds°¡ 0ÀÏ ¶§ Ã³¸®
            if (Mathf.Approximately(posMin, posMax)) posMax = posMin + 0.001f;

            // 6. Position Texture »ý¼º (RGBAHalf)
            var posTex = new Texture2D(vertCount, totalFrames, TextureFormat.RGBAHalf, false);
            posTex.filterMode = FilterMode.Bilinear;
            posTex.wrapMode = TextureWrapMode.Clamp;

            var posColors = new Color[vertCount * totalFrames];
            float range = posMax - posMin;

            for (int f = 0; f < totalFrames; f++)
            {
                for (int v = 0; v < vertCount; v++)
                {
                    Vector3 offset = allOffsets[f * vertCount + v];
                    // [posMin, posMax] ¡æ [0, 1] Á¤±ÔÈ­
                    float r = (offset.x - posMin) / range;
                    float g = (offset.y - posMin) / range;
                    float b = (offset.z - posMin) / range;
                    posColors[f * vertCount + v] = new Color(r, g, b, 1f);
                }
            }
            posTex.SetPixels(posColors);
            posTex.Apply();

            // 7. Normal Texture »ý¼º (RGBAHalf, [-1,1] ¡æ [0,1])
            Texture2D normTex = null;
            if (_bakeNormals)
            {
                normTex = new Texture2D(vertCount, totalFrames, TextureFormat.RGBAHalf, false);
                normTex.filterMode = FilterMode.Bilinear;
                normTex.wrapMode = TextureWrapMode.Clamp;

                var normColors = new Color[vertCount * totalFrames];
                for (int i = 0; i < allNormals.Length; i++)
                {
                    Vector3 n = allNormals[i];
                    normColors[i] = new Color(
                        n.x * 0.5f + 0.5f,
                        n.y * 0.5f + 0.5f,
                        n.z * 0.5f + 0.5f,
                        1f);
                }
                normTex.SetPixels(normColors);
                normTex.Apply();
            }

            // 8. UV1¿¡ VertexID ±Á±â (Shader Graph¿¡¼­ GetVertexID Custom Node ºÒÇÊ¿ä)
            //    X = VertexID (Á¤¼ö), Y = 0
            var vatMesh = Instantiate(smr.sharedMesh);
            // ³ë¸Ö/ÅºÁ¨Æ® ¸í½ÃÀû º¹»ç (Instantiate ÈÄ À¯½Ç ¹æÁö)
            vatMesh.normals = smr.sharedMesh.normals;
            vatMesh.tangents = smr.sharedMesh.tangents;
            var vertexIDs = new Vector2[vertCount];
            for (int v = 0; v < vertCount; v++)
                vertexIDs[v] = new Vector2(v, 0f);
            vatMesh.uv2 = vertexIDs;

            // 9. ÀúÀå
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);

            string baseName = $"{_targetObject.name}_{_clip.name}";
            SaveTexture(posTex, Path.Combine(_savePath, $"{baseName}_pos.asset"));
            if (_bakeNormals)
                SaveTexture(normTex, Path.Combine(_savePath, $"{baseName}_norm.asset"));

            // VAT Àü¿ë ¸Þ½Ã ÀúÀå (UV1 Æ÷ÇÔ)
            string meshPath = Path.Combine(_savePath, $"{baseName}_mesh.asset");
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh != null)
            {
                EditorUtility.CopySerialized(vatMesh, existingMesh);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(vatMesh, meshPath);
            }

            // 10. ¸ÞÅ¸ µ¥ÀÌÅÍ ÀúÀå (¼ÎÀÌ´õ¿¡¼­ º¹¿ø¿¡ ÇÊ¿ä)
            SaveMeta(baseName, posMin, posMax, vertCount, totalFrames, _fps);


            // 11. ÀÓ½Ã ¸Þ½Ã Á¤¸®
            DestroyImmediate(bakedMesh);

            AssetDatabase.Refresh();
            _statusMessage = $"? ¿Ï·á! [{baseName}] pos/norm ÅØ½ºÃ³ ÀúÀåµÊ\n" +
                             $"bounds: [{posMin:F4}, {posMax:F4}]  |  {vertCount}verts ¡¿ {totalFrames}frames";
        }
        catch (System.Exception e)
        {
            _statusMessage = $"? ¿À·ù: {e.Message}";
            Debug.LogException(e);
        }
        finally
        {
            _isBaking = false;
            Repaint();
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇïÆÛ : Æ¯Á¤ ½Ã°£¿¡ ¸Þ½Ã º£ÀÌÅ·
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private static void SampleFrame(
        SkinnedMeshRenderer smr,
        GameObject rootGO,
        AnimationClip clip,
        float time,
        Mesh outMesh)
    {
        // Å¬¸³À» ¿ÀºêÁ§Æ®¿¡ Á÷Á¢ Àû¿ë ¡æ ÈÞ¸Ó³ëÀÌµå Æ÷ÇÔ Áï½Ã ¹Ý¿µ
        clip.SampleAnimation(rootGO, time);
        smr.BakeMesh(outMesh, true);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇïÆÛ : Texture2D ¡æ .asset ÀúÀå
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private static void SaveTexture(Texture2D tex, string path)
    {
        // ±âÁ¸ ¿¡¼ÂÀÌ ÀÖÀ¸¸é µ¤¾î¾²±â
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(tex, existing);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(tex, path);
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇïÆÛ : ¼ÎÀÌ´õ º¹¿ø¿¡ ÇÊ¿äÇÑ ¸ÞÅ¸ µ¥ÀÌÅÍ¸¦ JSONÀ¸·Î ÀúÀå
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SaveMeta(string baseName, float posMin, float posMax,
                          int vertCount, int totalFrames, int fps)
    {
        var meta = new VATMeta
        {
            clipName = _clip.name,
            posMin = posMin,
            posMax = posMax,
            posRange = posMax - posMin,
            vertexCount = vertCount,
            frameCount = totalFrames,
            fps = fps,
            clipLength = _clip.length
        };

        string json = JsonUtility.ToJson(meta, true);
        string path = Path.Combine(_savePath, $"{baseName}_meta.json");
        File.WriteAllText(path, json);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸ÞÅ¸ µ¥ÀÌÅÍ ±¸Á¶Ã¼
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [System.Serializable]
    private class VATMeta
    {
        public string clipName;
        public float posMin;
        public float posMax;
        public float posRange;
        public int vertexCount;
        public int frameCount;
        public int fps;
        public float clipLength;
    }
}