using Dreamscape;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{
    
    public class DMX_Linear_Actuator : DMX_device
    {
        //******************* NOTES *****************
        //Channel 1 is used for fine small movement, but since we are going from end to end in AZ
        //No need to use channel 1.  The unit takes a Speed and Acceleration parameter, right now 
        //they are tied together, channel 2 & 3.  

        [Header("Slider Specific Variables")]
        //AZ Suggested Acceperation 0.5f
        public float Acceleration;
        public float Position;
        public float Velocity;

        private float _Acceleration;
        private float _Position;
        private float _Velocity;

        public new void Reset()
        {
            base.Reset();
            dmxDevice.deviceType = "DMX_Slider";
        }

        public new void Update()
        {
            speed = Mathf.Clamp(speed, 0, 1);
            _dmxValue = speed * 255.0f;
            Position = Mathf.Clamp(Position, 0, 1);
            _Position = Position * 255.0f;
            Velocity = Mathf.Clamp(Velocity, 0, 1);
            _Velocity = Velocity * 255.0f;
            Acceleration = Mathf.Clamp(Acceleration, 0, 1);
            _Acceleration = Acceleration * 255.0f;
            

            DefaultUniformChanneleUpdate();
            UpdateSubDevicesAndSlaveDevices();
        }

        public new void DefaultUniformChanneleUpdate()
        {
            int count = dmxDevice.getChannelCount();

            //Slider Specific code
            if(count != 4)
            {
                Debug.LogError("Doesn't have the correct amount of channels");
                return;
            } else
            {
                for(int i = 0; i < count; i++)
                {
                    switch(i)
                    {
                        case 0:
                            dmxDevice.setDMXChannelValue(i, _Position);
                            break;

                        case 1:
                            dmxDevice.setDMXChannelValue(i, 0);
                            break;

                        case 2:
                            dmxDevice.setDMXChannelValue(i, _Velocity);
                            break;

                        case 3:
                            dmxDevice.setDMXChannelValue(i, _Acceleration);
                            break;

                    }
                    
                }
            }
        }
    }
}

