import os
import subprocess
import time
import configparser

def check_and_connect_to_wifi(network_name):
    try:
        result = subprocess.run("netsh wlan show interfaces", capture_output=True, text=True)
        if "Software Off" in result.stdout:
            print("Wi-Fi is turned off. Please turn it on.")
            return False
        elif f"SSID                   : {network_name}" in result.stdout and "State                  : connected" in result.stdout:
            print(f"{network_name} is already connected")
        else:
            subprocess.run("netsh wlan disconnect", stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            print("Disconnected from current Wi-Fi network")
            subprocess.run(f"netsh wlan connect name={network_name}", stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            print("Establishing wifi connection")
            time.sleep(5)
            print("Wifi connection established")
    except Exception as e:
        print(f"An error occurred when connecting to the Wi-Fi: {e}")
        return False

    return True

def remote_directory_exists(hostname, username, password, directory):
    try:
        check_directory_command = f"plink -batch -pw {password} {username}@{hostname} \"if exist {directory} (echo 1) else (echo 0)\""
        result = subprocess.run(check_directory_command, capture_output=True, text=True)
        return int(result.stdout.strip()) == 1
    except Exception as e:
        print(f"An error occurred when checking remote directory existence: {e}")
        return False

wifi_network = "ASU01-02"
if not check_and_connect_to_wifi(wifi_network):
    print("Error in connecting to the Wi-Fi network. Please check the network and try again.")
    exit(1)


config = configparser.ConfigParser()
config.read('config.ini')
user_directory = config['Settings']['UserDirectory']
fixed_path = "D:\\Development\\Experiences\\"
destination = fixed_path + user_directory
directory = r"Experience"
directoryzip = f"{directory}.tar.gz"
remote_sdk_directory = r"D:\Development\SDK"

hostnames = [
    "10.1.2.20",
]

username = "artanim"
password = "artanim0"

remote_cmd_destination = f"cd /D {destination}"
remote_unzip = r"tar -xvf Experience.tar.gz"
remote_cmd_directory = r"cd /D D:\upload_build"
remote_cmd = f"python upload_build.py {destination} --server http://10.1.201.102:5000/ --compress_level 1"
open_url_cmd = r"start msedge http://10.1.201.102:5000/"
tightvnc_cmd = r"C:\Program Files\TightVNC\tvnviewer.exe -host=10.1.2.20 -password=crvr"

if not os.path.exists(directory):
    print(f"Error: The directory '{directory}' does not exist. Please check and try again.")
    exit(1)

try:
    print("Destination Found from Config File is:" + destination)
    print("Compressing build...")
    compression_command = f"tar -cvzf {directoryzip} {directory}"
    subprocess.run(compression_command, check=True)
    print("Build zipped")

    for hostname in hostnames:
        print(f"Checking if this build exists on server : {hostname}")
        if remote_directory_exists(hostname, username, password, destination):
            print("Destination directory already exists on the server.")
        else:
            print("Destination directory does not exist. Creating it.")
            create_directory_command = f"plink -batch -pw {password} {username}@{hostname} \"mkdir {destination}\""
            subprocess.run(create_directory_command, check=True)
            copy_directory_command = f"plink -batch -pw {password} {username}@{hostname} \"xcopy {remote_sdk_directory} {destination}\\{os.path.basename(remote_sdk_directory)} /E /I\""
            subprocess.run(copy_directory_command, check=True)
            print("Added SDK to the newly created destination directory.")

        print("Transferring build to Server...")
        transfer_command = f"pscp -r -pw {password} {directoryzip} {username}@{hostname}:{destination}"
        subprocess.run(transfer_command, check=True)
        print("Build transfer to POD - 2 Server completed.")

        print("Transferring Build from Server to DevDeploy...")
        remote_commands = f"{remote_cmd_destination} && {remote_unzip} && {remote_cmd_directory} && {remote_cmd}"
        remote_command = f"plink -batch -pw {password} {username}@{hostname} \"{remote_commands}\""    
        subprocess.run(remote_command, check=True)

        remote_commands2 = f"{open_url_cmd}"
        remote_command2 =  f"plink -batch -pw {password} {username}@{hostname} \"{remote_commands2}\""
        subprocess.run(remote_command2, check=True)

        print("Build Transferred to DevDeploy completed.")
        subprocess.run(tightvnc_cmd, check=True)
        
    print("The Deployment process completed successfully.")
    print("You can now close this window.")

except Exception as e:
    print(f"An unexpected error occurred during Deployment: {e}")
    print("The Deployment process did not complete successfully. Please check the error message and try again.")
    exit(1)