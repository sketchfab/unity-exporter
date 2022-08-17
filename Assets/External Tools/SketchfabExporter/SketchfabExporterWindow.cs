using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using SimpleJSON;
using Ionic.Zip;

struct ObjMaterial
{
    public string name;
    public string textureName;
    public string bumpTexture;
}

public class SketchfabExporterWww : MonoBehaviour {
	private string api_url = "https://api.sketchfab.com/v1/models";
	private WWW www = null;
	
	public IEnumerator UploadFileCo(string localFileName, string token, bool model_private, string title, string description, string tags)
    {
        //#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX  edit: removed  Platform dependency throwing error on oculus builds
        byte[] data = File.ReadAllBytes(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + localFileName);
		if (data.Length > 0) {
			Debug.Log("Loaded file successfully : " + data.Length + " bytes");
		} else {
			Debug.Log("Open file error");
			yield break;
		}

		
		WWWForm postForm = new WWWForm();
		postForm.AddBinaryData("fileModel",data,localFileName, "application/zip");
		postForm.AddField("title", title);
		postForm.AddField("description", description);
		postForm.AddField("filenameModel", "Unity.zip");
		postForm.AddField("source", "Unity-exporter");
		postForm.AddField("tags", tags);
		postForm.AddField("token", token);
		postForm.AddField("private", model_private ? "1" : "0");
		
		www = new WWW(api_url, postForm);        
		
        //#endif
        yield return www;

        }
	
	public string getUrlID() {
		if (www.error == null) {
			// test result
			var N = JSON.Parse(www.text);
			if (N["success"].AsBool == true) {
				return N["result"]["id"].Value;
			}
		}
		return null;
	}
	
	public bool done() {
		return www != null && www.isDone;
	}

	public float progress() {
		if (www == null)
			return 0.0f;
		
		return 0.99f * www.uploadProgress + 0.01f * www.progress;
	}
	
	public string errored() {
		if (www.error == null) {
			var N = JSON.Parse(www.text);
			
			return N["success"].AsBool == true ? null : N["error"].Value;;
		} else {
			return "Post failed: " + www.error;
		}
	}
	
	public void upload(string filename, string token, bool model_private, string title, string description, string tags) {
		StartCoroutine(UploadFileCo(filename, token, model_private, title, description, tags));
	}
}

public class SketchfabExporter
{
	private const string api_url = "https://api.sketchfab.com/v1/models";
	
	private string param_title;
	private string param_description;
	private string param_tags;
	private bool param_private;
	private string param_password;
	private string param_token;
	private ArrayList meshList;
	
	private static ZipFile zip;
	private SketchfabExporterWww exporterWww = null;
	private const string zipname = "ExportedObj.zip";
	private static string static_token;
	private static int vertexOffset = 0;
	private static int normalOffset = 0;
	private static int uvOffset = 0;
	private string exportDirectory;
	private static string targetFolder = "ExportedObj";
	
	public SketchfabExporter(string token, ArrayList m, string title, string description, string tags, bool priv, string password) {
		param_token = token;
		meshList = m;
		param_title = title;
		param_description = description;
		param_tags = tags;
		param_private = priv;
		param_password = password;
	}

	public void export() {
		exportDirectory = Application.temporaryCachePath + Path.DirectorySeparatorChar + "SketchfabExport";
		clean();
		zip = new ZipFile();

		FileInfo fi = new FileInfo(EditorApplication.currentScene);
		string filename = fi.Name + "_" + meshList.Count;
		MeshesToFile(meshList, exportDirectory, filename);
		System.IO.Directory.CreateDirectory(exportDirectory);


		// create zip
		zip.Save(zipname);
		Debug.Log("Created archive " + zipname);
		
		// upload zip to sketchfab
		GameObject go = Selection.activeObject as GameObject;
		exporterWww = go.AddComponent<SketchfabExporterWww>();
		exporterWww.upload(zipname, param_token, param_private, param_title, param_description, param_tags);
	}

	private static string SkinnedMeshRendererFilterToString(SkinnedMeshRenderer mf, Dictionary<string, ObjMaterial> materialList)
	{
		Mesh m = mf.sharedMesh;
		Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
		
		StringBuilder sb = new StringBuilder();
		
		sb.Append("g ").Append(mf.name).Append("\n");
		foreach(Vector3 lv in m.vertices) {
			Vector3 wv = mf.transform.TransformPoint(lv);
			
			//This is sort of ugly - inverting x-component since we're in
			//a different coordinate system than "everyone" is "used to".
			sb.Append(string.Format("v {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 lv in m.normals) {
			Vector3 wv = mf.transform.TransformDirection(lv);
			
			sb.Append(string.Format("vn {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 v in m.uv) {
			sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
		}
		
		return MeshToString(m, mats, sb, materialList);
	}
	
	private static string MeshFilterToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList) 
	{
		Mesh m = mf.sharedMesh;
		Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
		
		StringBuilder sb = new StringBuilder();
		
		sb.Append("g ").Append(mf.name).Append("\n");
		foreach(Vector3 lv in m.vertices) {
			Vector3 wv = mf.transform.TransformPoint(lv);
			
			//This is sort of ugly - inverting x-component since we're in
			//a different coordinate system than "everyone" is "used to".
			sb.Append(string.Format("v {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 lv in m.normals) {
			Vector3 wv = mf.transform.TransformDirection(lv);
			
			sb.Append(string.Format("vn {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 v in m.uv) {
			sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
		}
		
		return MeshToString(m, mats, sb, materialList);
	}
	
	private static string MeshToString(Mesh m, Material[] mats, StringBuilder sb, Dictionary<string, ObjMaterial> materialList) 
	{
		for (int material=0; material < m.subMeshCount; material ++) {
			sb.Append("\n");
			if (mats[material] != null) {
				sb.Append("usemtl ").Append(mats[material].name).Append("\n");
				sb.Append("usemap ").Append(mats[material].name).Append("\n");
				
				//See if this material is already in the materiallist.
				try {
					ObjMaterial objMaterial = new ObjMaterial();
					
					objMaterial.name = mats[material].name;
					
					if (mats[material].mainTexture)
						objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
					else 
						objMaterial.textureName = null;
					
					objMaterial.bumpTexture = null;
					Texture bumpTexture = null;
					try {
						bumpTexture = mats[material].GetTexture("_BumpMap");
					} catch {
					}
					if(bumpTexture) {
						objMaterial.bumpTexture = AssetDatabase.GetAssetPath(bumpTexture);
					}
					
					materialList.Add(objMaterial.name, objMaterial);
				} catch (ArgumentException) {
					//Already in the dictionary
				}
			}
			
			int[] triangles = m.GetTriangles(material);
			for (int i=0;i<triangles.Length;i+=3) {
				//Because we inverted the x-component, we also needed to alter the triangle winding.
				sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n", 
				                        triangles[i]+1 + vertexOffset, triangles[i+1]+1 + normalOffset, triangles[i+2]+1 + uvOffset));
			}
		}
		
		vertexOffset += m.vertices.Length;
		normalOffset += m.normals.Length;
		uvOffset += m.uv.Length;
		
		return sb.ToString();
	}
	
	private static void Clear()
	{
		vertexOffset = 0;
		normalOffset = 0;
		uvOffset = 0;
	}
	
	private static Dictionary<string, ObjMaterial> PrepareFileWrite()
	{
		Clear();
		
		return new Dictionary<string, ObjMaterial>();
	}
	
	private static void MaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
	{
		string path = folder + Path.DirectorySeparatorChar + filename + ".mtl";
		using (StreamWriter sw = new StreamWriter(path)) {
			foreach(KeyValuePair<string, ObjMaterial> kvp in materialList) {
				sw.Write("\n");
				sw.Write("newmtl {0}\n", kvp.Key);
				sw.Write("Ka  0.6 0.6 0.6\n");
				sw.Write("Kd  0.6 0.6 0.6\n");
				sw.Write("Ks  0.1 0.1 0.1\n");
				sw.Write("d  1.0\n");
				sw.Write("Ns  150.0\n");
				sw.Write("illum 2\n");
				
				if (kvp.Value.textureName != null) {
					string destinationFile = kvp.Value.textureName;
					int stripIndex = destinationFile.LastIndexOf(Path.DirectorySeparatorChar);
					if(stripIndex == -1)
						stripIndex = destinationFile.LastIndexOf('/');
					if(stripIndex == -1)
						stripIndex = destinationFile.LastIndexOf('\\');
					if (stripIndex >= 0)
						destinationFile = destinationFile.Substring(stripIndex + 1).Trim();
					
					try {
						zip.AddFile(kvp.Value.textureName, "");
					} catch {
					}
					
					sw.Write("map_Kd {0}\n", destinationFile);
				}
				
				if (kvp.Value.bumpTexture != null) {
					string destinationFile = kvp.Value.bumpTexture;
					int stripIndex = destinationFile.LastIndexOf(Path.DirectorySeparatorChar);
					if(stripIndex == -1)
						stripIndex = destinationFile.LastIndexOf('/');
					if(stripIndex == -1)
						stripIndex = destinationFile.LastIndexOf('\\');
					if (stripIndex >= 0)
						destinationFile = destinationFile.Substring(stripIndex + 1).Trim();
					
					try {
						zip.AddFile(kvp.Value.bumpTexture, "");
					} catch {
					}
					
					sw.Write("map_Bump {0}\n", destinationFile);
				}
				
				sw.Write("\n\n\n");
			}
		}
		zip.AddFile(path, "");
	}
	
	private static void MeshesToFile(ArrayList meshes, string folder, string filename) 
	{
		Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();
		
		System.IO.Directory.CreateDirectory(folder);
		string path = folder + Path.DirectorySeparatorChar + filename + ".obj";
		using (StreamWriter sw = new StreamWriter(path)) {
			sw.Write("mtllib ." + Path.DirectorySeparatorChar + filename + ".mtl\n");
			
			for (int i = 0; i < meshes.Count; i++) {
				if(meshes[i] is MeshFilter) {
					MeshFilter mf = (MeshFilter)meshes[i];
					sw.Write(MeshFilterToString(mf, materialList));
				} else if(meshes[i] is SkinnedMeshRenderer) {
					SkinnedMeshRenderer smr = (SkinnedMeshRenderer)meshes[i];
					sw.Write(SkinnedMeshRendererFilterToString(smr, materialList));
				}
			}
		}
		zip.AddFile(path, "");
		
		MaterialsToFile(materialList, folder, filename);
	}
	
	private static bool CreateTargetFolder() {
		try {
			System.IO.Directory.CreateDirectory(targetFolder);
		} catch {
			EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "");
			return false;
		}
		
		return true;
	}

	public bool done() {
		if(exporterWww == null)
			return false;
		return exporterWww.done();
	}

	public string errored() {
		if(exporterWww == null)
			return null;
		return exporterWww.errored();
	}

	public string getUrlID() {
		if(exporterWww == null)
			return null;
		return exporterWww.getUrlID();
	}

	public float progress()	{
		if(exporterWww == null)
			return 0.0f;
		return exporterWww.progress();
	}

	public void clean() {
		if (System.IO.File.Exists(zipname))
			System.IO.File.Delete(zipname);
	}
}

public class SketchfabExporterWindow : EditorWindow
{
	private string param_title = "Unity model";
	private string param_description = "Model exported from Unity Engine.";
	private string param_tags = "unity";
	private bool param_private = false;
	private string param_password = "";
	private string param_token = "";

	private static string dashboard_url = "https://sketchfab.com/dashboard";
	private SketchfabExporter exporter;
	private bool finished = false;

    [MenuItem("Window/Export selection to Sketchfab...")]
	static void Init()
    {
    #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone
        SketchfabExporterWindow window = (SketchfabExporterWindow)EditorWindow.GetWindow (typeof(SketchfabExporterWindow));
		window.initialize();
    #else // and error dialog if not standalone
        EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
    #endif
    }



	void initialize() {
		FileInfo fi = new FileInfo(EditorApplication.currentScene);
		param_title = fi.Name;
	}
	
	void export() {
		finished = false;
		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
		if (selection.Length == 0) {
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "Okay :(");
			return;
		}
		
		if (param_token.Trim().Length == 0) {
			EditorUtility.DisplayDialog("Invalid token!", "Your Sketchfab API Token identifies yourself to Sketchfab. You can get this token at https://sketchfab.com/dashboard.", "");
			return;
		}

		ArrayList meshList = new ArrayList();
		for (int i = 0; i < selection.Length; i++) {
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));
			for (int m = 0; m < meshfilter.Length; m++) {
				meshList.Add(meshfilter[m]);
			}
			
			Component[] skinnermeshrenderer = selection[i].GetComponentsInChildren(typeof(SkinnedMeshRenderer));
			for (int m = 0; m < skinnermeshrenderer.Length; m++) {
				meshList.Add(skinnermeshrenderer[m]);
			}
		}
		
		if (meshList.Count > 0) {
			exporter = new SketchfabExporter(param_token, meshList, param_title, param_description, param_tags, param_private, param_password);
			exporter.export();
		} else {
			EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "Okay :(");
		}
	}
	
	void OnGUI() {
		GUILayout.Label("Model settings", EditorStyles.boldLabel);
		param_title = EditorGUILayout.TextField("Title (Scene name)", param_title); //edit: added name source
		param_description = EditorGUILayout.TextField("Description", param_description);
		param_tags = EditorGUILayout.TextField("Tags", param_tags);
        
        // edit: contained the password field in a toggle group
        param_private = EditorGUILayout.BeginToggleGroup("Private", param_private);
		param_password = EditorGUILayout.PasswordField("Password", param_password);
        EditorGUILayout.EndToggleGroup();

		GUILayout.Label("Sketchfab settings", EditorStyles.boldLabel);
		param_token = EditorGUILayout.TextField("API Token", param_token);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("find your token");
		if(GUILayout.Button("open dashboard"))
			Application.OpenURL(dashboard_url);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		if(exporter != null && exporter.done() == false) {
			GUI.enabled = false;
		}
		if(GUILayout.Button("Upload to Sketchfab")) {
			export();
		}
		GUI.enabled = true;

		if(exporter != null && exporter.done() == false) {
			Rect r = EditorGUILayout.BeginVertical();
			EditorGUI.ProgressBar(r, exporter.progress(), "Upload progress");
			GUILayout.Space(16);
			EditorGUILayout.EndVertical();
		}
	}

	public void OnInspectorUpdate()	{
		Repaint();

		if(exporter != null && finished == false && exporter.done() == true) {
			finished = true;

			string error = exporter.errored();
			if(error == null) {
				string urlid = exporter.getUrlID();
				string modelUrl = "http://sketchfab.com/show/" + urlid;
				
				EditorUtility.DisplayDialog("Success", "Model has been successfully uploaded to sketchfab !", "Watch it");
				Application.OpenURL(modelUrl);
				exporter.clean();
			} else {
				EditorUtility.DisplayDialog("Error", error, "Okay :(");
			}
		}
	}
}
