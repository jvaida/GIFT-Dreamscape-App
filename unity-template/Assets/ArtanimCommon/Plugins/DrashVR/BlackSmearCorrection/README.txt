LICENSE: 
Public domain, no attribution necessary.

NOTE: 
This is an unofficial and temporary solution to correcting for the black smear present in the Oculus Rift DK2 in scenes containing completely black areas.  
If and when Oculus releases an official way to correct for this, that should be used instead.

HOW TO USE:

1. 
Drop a "BlackSmearCorrectionEffect" script onto each of your cameras.  
If you're using a Rift camera, find the CameraLeft and CameraRight cameras and drop this script onto each one.  
You may also drop it onto normal cameras as well.

2. 
Drop a "BlackSmearCorrection" onto an active part of your scene's Hierarchy that is somewhere up the tree from your cameras.  
For example you can drop this script onto the OVRCameraController or OVRPlayerController, 
which is a parent/grandparent/ancestor/etc of the two cameras.

3. 
The "BlackSmearCorrection" component should already be configured with reasonable defaults: 
-2 Contrast, +10 Brightness, 0.1 Smooth Time, Initially applying correction, and F10 as the key to toggle black smear correction at runtime.   
Feel free to change any of these as needed.


That's it.  Hope it helps!
