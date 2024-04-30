# Dreamscape Setup for New Computers


## Dreamscape SDK Setup



1. Download the [SDK zip file](https://www.dropbox.com/s/jiaftnspbm09pda/SDK-6.5.9-20220902-092135.zip) and **extract **it somewhere onto your computer (Documents folder is good).
    1. **Do not distribute the SDK files outside of Meteor.**
    2. SDK files should be located in the same folder as this document if the link is broken

<br />

2. Go inside the unzipped folder:
    1. Go to the /install sub-folder
    2. Install all four of the .exe files included. (Ignore any “duplicate install” errors, chances are you already have some of them installed.)

<br />

3. Open the Start Menu and search for “Edit the system environment variables”. Open the window and click Environment Variables.

    ![alt_text](images/image3.png "System Properties")

    ![alt_text](images/image2.png "Environment Variables")

<br />

4. Add a new **SYSTEM** environment **variable** named **ARTANIM_HOME**<br />
    Click Browse Directory and set the target inside your unzipped SDK folder, to the **subfolder** \runtime\home

    ![alt_text](images/image1.png "Add System Variable")

<br />

5. (if you haven't already) Download and install Unity 2021.3.37f LTS, along with Visual Studio.

<br />

6. **<span style="text-decoration:underline;">Restart your computer. (REQUIRED)</span>**

<br />

## Potential Dreamscape errors and respective fixes

- ```
    Assets\ArtanimCommon\Tracking\ViconConnector.cs(7,22): error CS0234: The type or namespace name 'Remoting' does not exist in the namespace 'System.Runtime' (are you missing an assembly reference?)
    ```
    Change Build Settings> Player Settings>Player> Other Settings>API Compatibility Level  to **.NET 4.x**

<br />

- ```
    Assets\ArtanimCommon\HandAnimation\Plugins\LeapMotion\Core\Scripts\Utils\Editor\Hotkeys.cs(122,91): error CS0619: 'SelectionMode.OnlyUserModifiable' is obsolete: 'OnlyUserModifiable' is obsolete. Use 'Editable' instead. (UnityUpgradeable) -> Editable
    ```

    Add to lines 25, 107, and 122 of `Hotkeys.cs`

    - ```cs
        // GameObject[] objs = Selection.GetFiltered&lt;GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
        GameObject[] objs = Selection.GetFiltered&lt;GameObject>(SelectionMode.ExcludePrefab | SelectionMode.TopLevel | SelectionMode.Editable);
        ```

<br />

- ```
    error CS0234: The type or namespace name 'SpatialTracking' does not exist in the namespace 'UnityEngine' (are you missing an assembly reference?)
    ```

    Import the "XR Legacy Input Helpers" package from Window -> Package Manager.
