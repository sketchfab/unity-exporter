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

public class Sketchfab : MonoBehaviour {
	private string url = "https://api.sketchfab.com/v1/models";
	private WWW www = null;
	
	public IEnumerator UploadFileCo(string localFileName, string token, bool model_private, string title, string description, string tags)
    {
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

        www = new WWW(url, postForm);        

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

public class EditorSketchfabExporter : ScriptableWizard
{
	public string token = "";
	public bool model_private = true;
	public string model_title = "Unity";
	public string model_description = "Model exported from Unity Engine.";
	public string model_tags = "unity";
	
	
	private Sketchfab sketchfab;
	private const string zipname = "ExportedObj.zip";
	private static string static_token;
	private static int vertexOffset = 0;
	private static int normalOffset = 0;
	private static int uvOffset = 0;
	private bool finished = false;
	private string exportDirectory;
 
	private static string targetFolder = "ExportedObj";
 
	private static ZipFile zip;
	
	public EditorSketchfabExporter() {
		if(static_token != null && static_token.Length > 0)
			token = static_token;
		
		FileInfo fi = new FileInfo(EditorApplication.currentScene);
		model_title = fi.Name;
	}
	
	private static string SkinnedMeshRendererFilterToString(SkinnedMeshRenderer mf, Dictionary<string, ObjMaterial> materialList)
	{
        Mesh m = mf.sharedMesh;
        Material[] mats = mf.renderer.sharedMaterials;
 
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(mf.name).Append("\n");
        foreach(Vector3 lv in m.vertices) 
        {
        	Vector3 wv = mf.transform.TransformPoint(lv);
 
        	//This is sort of ugly - inverting x-component since we're in
        	//a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n",-wv.x,wv.y,wv.z));
        }
        sb.Append("\n");
 
        foreach(Vector3 lv in m.normals) 
        {
        	Vector3 wv = mf.transform.TransformDirection(lv);
 
            sb.Append(string.Format("vn {0} {1} {2}\n",-wv.x,wv.y,wv.z));
        }
        sb.Append("\n");
 
        foreach(Vector3 v in m.uv) 
        {
            sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
        }
		
		return MeshToString(m, mats, sb, materialList);
	}
	
	private static string MeshFilterToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList) 
    {
        Mesh m = mf.sharedMesh;
        Material[] mats = mf.renderer.sharedMaterials;
 
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(mf.name).Append("\n");
        foreach(Vector3 lv in m.vertices) 
        {
        	Vector3 wv = mf.transform.TransformPoint(lv);
 
        	//This is sort of ugly - inverting x-component since we're in
        	//a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n",-wv.x,wv.y,wv.z));
        }
        sb.Append("\n");
 
        foreach(Vector3 lv in m.normals) 
        {
        	Vector3 wv = mf.transform.TransformDirection(lv);
 
            sb.Append(string.Format("vn {0} {1} {2}\n",-wv.x,wv.y,wv.z));
        }
        sb.Append("\n");
 
        foreach(Vector3 v in m.uv) 
        {
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
	            try
	       		{
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
	        	}
	        	catch (ArgumentException)
	        	{
	            	//Already in the dictionary
	        	}
			}
	 
            int[] triangles = m.GetTriangles(material);
            for (int i=0;i<triangles.Length;i+=3) 
            {
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
   		using (StreamWriter sw = new StreamWriter(path)) 
        {
        	foreach( KeyValuePair<string, ObjMaterial> kvp in materialList )
        	{
        		sw.Write("\n");
        		sw.Write("newmtl {0}\n", kvp.Key);
        		sw.Write("Ka  0.6 0.6 0.6\n");
				sw.Write("Kd  0.6 0.6 0.6\n");
				sw.Write("Ks  0.1 0.1 0.1\n");
				sw.Write("d  1.0\n");
				sw.Write("Ns  150.0\n");
				sw.Write("illum 2\n");
 
				if (kvp.Value.textureName != null)
				{
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
				
				if (kvp.Value.bumpTexture != null)
				{
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
 
		string path = folder + Path.DirectorySeparatorChar + filename + ".obj";
        using (StreamWriter sw = new StreamWriter(path)) 
        {
        	sw.Write("mtllib ." + Path.DirectorySeparatorChar + filename + ".mtl\n");
 
        	for (int i = 0; i < meshes.Count; i++)
        	{
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
 
    private static bool CreateTargetFolder()
    {
    	try
    	{
    		System.IO.Directory.CreateDirectory(targetFolder);
    	}
    	catch
    	{
    		EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "");
    		return false;
    	}
 
    	return true;
    }

    [MenuItem ("Custom/Export/Export to Sketchfab")]
    static void ExportToSketchfab()
    {
		ScriptableWizard.DisplayWizard("Sketchfab export parameters...", typeof(EditorSketchfabExporter), "Cancel", "Upload");
        
	}

	void OnWizardCreate()
	{
	}
	
	void OnWizardOtherButton()
	{
		static_token = token;
		exportDirectory = Application.temporaryCachePath + Path.DirectorySeparatorChar + "SketchfabExport";
		clean();
		zip = new ZipFile();
 
        Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
 
        if (selection.Length == 0)
        {
        	EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "");
        	return;
        }
 
		// list selected meshes
        int exportedObjects = 0;
 		ArrayList meshList = new ArrayList();
       	for (int i = 0; i < selection.Length; i++) {
       		Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));
       		for (int m = 0; m < meshfilter.Length; m++) {
       			exportedObjects++;
       			meshList.Add(meshfilter[m]);
       		}
			
			Component[] skinnermeshrenderer = selection[i].GetComponentsInChildren(typeof(SkinnedMeshRenderer));
       		for (int m = 0; m < skinnermeshrenderer.Length; m++) {
       			exportedObjects++;
       			meshList.Add(skinnermeshrenderer[m]);
       		}
       	}
		
		if (exportedObjects > 0) {
			FileInfo fi = new FileInfo(EditorApplication.currentScene);
			string filename = fi.Name + "_" + exportedObjects;
			MeshesToFile(meshList, exportDirectory, filename);
			System.IO.Directory.CreateDirectory(exportDirectory);
			
			// create zip
			zip.Save(zipname);
			Debug.Log("Created archive " + zipname);

			// upload zip to sketchfab
			finished = false;
			GameObject go = Selection.activeObject as GameObject;
			sketchfab = go.AddComponent<Sketchfab>();
			sketchfab.upload(zipname, token, model_private, model_title, model_description, model_tags);
		} else {
			EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
		}
    }
	
	void clean()
	{
		if (System.IO.File.Exists(zipname))
			System.IO.File.Delete(zipname);
	}

	void Update()
	{
		if(sketchfab != null && finished == false && sketchfab.done()) {
			finished = true;
			
			string error = sketchfab.errored();
			if(error == null) {
				string urlid = sketchfab.getUrlID();
				string modelUrl = "http://sketchfab.com/show/" + urlid;
				
				EditorUtility.DisplayDialog("Success", "Model has been successfully uploaded to sketchfab !", "Watch it");
				Application.OpenURL(modelUrl);
				clean();
			} else {
				EditorUtility.DisplayDialog("Error", error, "Okay :(");
			}
		}
	}
}
