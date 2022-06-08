***************************************
*** TeamSpeak Unity Plugin - README ***
***************************************

Using this plugin you can use the TeamSpeak SDK 3.0.4 in Unity3D

In the folder TeamSpeak/Example you can find a simple example scene.
Please see Quickstart.pdf for a guide how to get the example scene running in Unity.

The example scene uses the class TS3_Client_Example defined in TeamSpeak/Example/TS3_Client_Example.cs.


You can call the functions directly using the static functions defined TeamSpeakInterface.cs.
This is not recommended since this means that you will have to manually release memory after calling some functions. If you still
want to do this, use the TeamSpeak SDK client manual to see where you need to release memory.

The TeamSpeakClient is a 'memory safe' wrapper for TeamSpeakInterface. All supported functions can be called trough this class.
By using this class you can easily use the callback functionality by editing the functions defined in TeamSpeakCallbacks.cs
Note that you first have to call TeamSpeakClient.StartClient or TeamSpeakClient.InitClientLib before you can use any other function.
The naming of the functions follow the names of the original SDK functions but without the ts3client_ prefix, and they start with a
uppercase character.

We recommend to use the auxiliary functions from the TeamSpeakClient class. In order to ensure platform independency, we recommend
the following:
- Use TeamSpeakClient.StartClient instead of connecting manually. This functions combines connecting to the server, and starting 
the sound devices for all supported platforms. Also, by using this function, the TeamSpeakClient will automatically store the
server connection handler.
- Use AddServerConnection instead of SpawnNewServerConnectionHandler. This function combines connecting to the server, and starting
the sound devices for all supported platforms. Also, by using this function, the TeamSpeakClient will automatically store the
server connection handler.
- You will see that there are a lot of functions where you can choose whether you want to specify the server connection handler id.
The server connection handler id is mandatory unless you have used StartClient and AddServerConnection. In this case the first
obtained connection handler will be used.

Custom password encryption is disabled by default. To enable it you have to uncomment the line '#define CUSTOM_PASSWORD_ENCRYPTION'
at the top of TeamSpeakCallbacks.cs. This is not supported for Android.

Notes for Android:
- When building for Android, you need to include the TeamSpeakAndroidEventHandler prefab into your scene. If this prefab is not
available, you will not receive any events.
- Custom password encryption is not supported on Android (Can't connect to a server using custom password encryption).

Notes for iOS:
- Unity 5: Only Scripting backend Mono2x is supported. You can find this option in Player Settings -> Other Settings.

Notes that not all TeamSpeak callbacks are supported in Unity:
- onCustomPacketEncryptEvent
- onCustomPacketDecryptEvent

The following callbacks aren't supported on Android:
- onEditPlaybackVoiceDataEvent
- onEditPostProcessVoiceDataEvent
- onEditMixedPlaybackVoiceDataEvent
- onEditCapturedVoiceDataEvent
- onCustom3dRolloffCalculationClientEvent
- onCustom3dRolloffCalculationWaveEvent
- onCheckServerUniqueIdentifierEvent
- onClientPasswordEncrypt

Note that the TeamSpeak SDK clients will only work with the TeamSpeak SDK servers. The original SDK (including the server part) can be found in
TeamSpeak/SDK folder.

*****************
*** CHANGELOG ***
*****************

2017/03/23
----------
- Upgrade to Teamspeak 3.0.4 SDK
- Plugin Build for Unity 5.5.2f1

2015/10/09
----------
- Upgrade to Teamspeak 3.0.3.2 SDK
- Support for Unity 5 (separate package for Unity 4 and Unity 5)


2015/05/21
----------

- Upgrade to the Teamspeak 3.0.3.1 SDK.
- TeamSpeakInterface.cs supports the following new functions:
  - ts3client_getChannelEmptySecs
  - ts3client_getTransferFileName
  - ts3client_getTransferFilePath
  - ts3client_getTransferFileRemotePath
  - ts3client_getTransferFileSize
  - ts3client_getTransferFileSizeDone
  - ts3client_isTransferSender
  - ts3client_getTransferStatus
  - ts3client_getCurrentTransferSpeed
  - ts3client_getAverageTransferSpeed
  - ts3client_getTransferRunTime
  - ts3client_sendFile
  - ts3client_requestFile
  - ts3client_haltTransfer
  - ts3client_requestFileList
  - ts3client_requestFileInfo
  - ts3client_requestDeleteFile
  - ts3client_requestCreateDirectory
  - ts3client_requestRenameFile
  - ts3client_getInstanceSpeedLimitUp
  - ts3client_getInstanceSpeedLimitDown
  - ts3client_getServerConnectionHandlerSpeedLimitUp
  - ts3client_getServerConnectionHandlerSpeedLimitDown
  - ts3client_getTransferSpeedLimit
  - ts3client_setInstanceSpeedLimitUp
  - ts3client_setInstanceSpeedLimitDown
  - ts3client_setServerConnectionHandlerSpeedLimitUp
  - ts3client_setServerConnectionHandlerSpeedLimitDown
  - ts3client_setTransferSpeedLimit
- TeamSpeakClient.cs now supports all the functions from TeamSpeakInterface. Functions called trough TeamSpeakClient don't require manually memory management.
- TeamSpeakCallbacks.cs contains the following new callbacks:
  - onSoundDeviceListChangedEvent
  - onEditPlaybackVoiceDataEvent
  - onEditPostProcessVoiceDataEvent
  - onEditMixedPlaybackVoiceDataEvent
  - onEditCapturedVoiceDataEvent
  - onCustom3dRolloffCalculationClientEvent
  - onCustom3dRolloffCalculationWaveEvent
  - onProvisioningSlotRequestResultEvent
  - onCheckServerUniqueIdentifierEvent
  - onClientPasswordEncryptEvent
  - onFileTransferStatusEvent
  - onFileListEvent
  - onFileListFinishedEvent
  - onFileInfoEvent