Sketchfab unity exporter
========================

The Sketchfab exporter for Unity is a script that helps uploading models from a Unity project to a Sketchfab model in just a few clicks.

Release date: Nov 22, 2013  
Last update: Aug 12, 2015  
Unity version: 5.1.1f1  
Author: Sketchfab, Clément Léger, Bryan Thatcher

Installation
------------
Download the unitypackage file and install it from *Assets → Import Package → Custom Package...*

Or download this repository and copy assets and its content into the root of your Unity project.

You should have the files :
- Assets/External Tools/SketchfabExporter/SketchfabExporterWindow.cs
- Assets/External Tools/SketchfabExporter/SimpleJSON.cs
- Assets/External Tools/SketchfabExporter/Ionic.Zip.dll

The Unity project is automatically updated and a new menu item will appear: *Window → Export Selection to Sketchfab...*

Usage
-----
You must save your scene before using the exporter.

Your project build settings shoud be set to *PC, Mac & Linux Standalone*. It won't work with Web Player, for example, because it does not allow manipulation of OBJ/ZIP files.

Select the objects you want to export - static geometry and skinned meshes. Basically, MeshFilter objects SkinnedMeshRenderer are exported which covers most static geometry and characters in T-pose.

Open the *Export Selection to Sketchfab...* window, add model metadata and your API token ( https://sketchfab.com/settings/password )

- Title
- Description
- Tags (space-sparated)
- Private (PRO and Business accounts only)
- Password (Optional for private models)

When the fields are set just press *Upload to Sketchfab* and wait, depending on the textures and your connection speed it could take some time until a message box appears informing you of the upload result.

Contact
-------
Please send your questions or feedback to support@sketchfab.com
