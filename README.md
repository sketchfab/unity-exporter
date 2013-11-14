Sketchfab unity exporter
========================

The Sketchfab exporter for Unity is a script that helps uploading models from a Unity project to a Sketchfab model in just a few clicks.

Release date: Nov 14, 2014  
Last update: Nov 14, 2014  
Unity version: 4.2.2f1  
Author: Clément Léger (clement@sketchfab.com), Sketchfab (support@sketchfab.com)

Installation
------------
Just copy the folder Assets and its content into the root of your Unity project.
You should have the files :
- Assets/External Tools/Exporter/EditorObjExporter.cs
- Assets/External Tools/Exporter/SimpleJSON.cs
- Assets/Ionic.Zip.dll

The Unity project is automatically updated so the installation is done when a menu "Custom" should appear and reveal a "Export to Sketchfab" option.

Usage
-----
Select in your scene the objects you want to export. Are exported static geometry but also skinned meshes.  
Basically, MeshFilter objects SkinnedMeshRenderer are exported which covers most static geometry and characters in T-pose.

Then open the Custom menu, then Export and finally click on "Export to Sketchfab".  
A window will open and some parameters are required in order to upload your model.

The token field matches the api key token you can find on your dashboard : http://sketchfab.com/dashboard and is required so the exporter links the model to your account.  
Following options are basic models properties : private if your account allows it, the model title, its description and a list of space separated tags.

When the fields are set just press the Upload button and wait, depending on the textures and your speed connection it could take some time until a message box appears informing you of the upload result.

Contact
-------
Please send your questions or feedback to support@sketchfab.com
