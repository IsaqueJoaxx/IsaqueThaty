SOLIDOS - plugin for device modeling, and sizing and simulate

__________________________________________________________________________________________
Author:

Neyton Luiz Dalle Molle
Civil engineer

__________________________________________________________________________________________
Installation:

1) Close Civil 3D if it is open

2) If you used the installer version:
   *First uninstall the previous version in the Control Panel*

3) If you loaded it with NETLOAD from any folder:
   *First rename the folder so that Civil 3D cannot find it

4) Unzip all the files in the same folder (it can be any folder)

5) Run the SETUP_SOLIDOS.exe installer with NORMAL user permissions,
   YOU DO NOT NEED TO BE AN ADMINISTRATOR

   If you use the default installation folder:
      C:\ProgramData\Autodesk\ApplicationPlugins\SOLIDOS.bundle\
   Civil 3D will load the program when opened

   If you choose a different folder, you must use the NETLOAD command to complete the
   installation. In this case, choose the dll compatible with the version of Civil 3D 
   you are using:

       Civil 3D DLL loading
    -------------------------------------------------
    Civil 3D 2020: ./dotnet_4/SOLIDOS_2020.dll

    Civil 3D 2021: ./dotnet_4/SOLIDOS_2021.dll
    Civil 3D 2022: ./dotnet_4/SOLIDOS_2021.dll
    Civil 3D 2023: ./dotnet_4/SOLIDOS_2021.dll
    Civil 3D 2024: ./dotnet_4/SOLIDOS_2021.dll

    Civil 3D 2025: ./dotnet_8/SOLIDOS_2025.dll
    Civil 3D 2026: ./dotnet_8/SOLIDOS_2025.dll
   
    Civil 2016 to 2019: ./dotnet_4/SOLIDOS_2020.dll
      Note: these versions are no longer officially supported!
      It may work, it may not!

6) Open the file "./Support/catalog.dwg" in the folder where you installed it to see
a catalog of devices already created

7) To load the menu and toolbars, use the CMENU command
   If it does not work, make sure that the folder and all subfolders and files
   where the plugin is installed are READ and WRITE
__________________________________________________________________________________________
Uninstall:

1) Close Civil 3D if it is open

2) Go to Control Panel, Add or Remove Programs, locate SOLIDOS and
    click the Uninstall option

__________________________________________________________________________________________
Troubleshooting installation:

1) Make sure that the folder and all subfolders and files where the plugin is
   installed are READ and WRITING

2) If the plugin does not load:
    Call the SECURELOAD command from the Civil 3D command line and enter 0,
    then restart Civil 3D

3) If you have other TBN2NET plugins (TBN2CAD, C3DRENESG4, C3DMEMO, TBN2C3D, DDM),
    It will be necessary to update everyone at https://tbn2net.com as conflicts may occur
    between DLL versions when loading into AutoCAD

4) If the DLL did not load, showing an error on the command line
   1) Locate the program installation folder
   2) Right-click on the DLL compatible with your version of CAD
   3) Click on Properties
   4) Click on the Unlock button, if it exists
   5) Restart CAD and try again with the NETLOAD command

5) If the help files (*.chm) do not open:
    1) Locate the program installation folder
    2) Right-click on the program's help files
    3) click on Properties
    4) Click the Unlock button, if there is one
   
    If the above procedure does not work:
       1) Open the command prompt as administrator (CMD.exe, from the start menu)
       2) Enter:
           regsvr32 %systemroot%\system32\hhctrl.ocx
           regsvr32 %systemroot%\system32\itss.dll


    Or, see the online help:

    https://tbn2net.com/help/SOLIDOSen

6) Menus or Ribbon do not appear after installation:
 Use the SMENU command

__________________________________________________________________________________________
Use license:

    Free to try for 15 days if you register a demo license

    To register, fill in your name and email on the screen that opens
    when using the program and that it is a demo version.

    After this period, the program returns to demonstration mode.

    If you purchase a permanent license, you will be subject to the
    PROGRAM LICENSE AGREEMENT, available in the program package.

__________________________________________________________________________________________
Warranties:

   None. Use at your own risk!!!

__________________________________________________________________________________________
Support:

     neyton@yahoo.com
     https://tbn2.blogspot.com
     https://tbn2net.com
     WhatsApp: +55 41 99827 5531
     skype: neyton@hotmail.com

Dudo que alguien haya leído este documento... Si lo hiciste, ¡¡¡envía un mensaje!!!