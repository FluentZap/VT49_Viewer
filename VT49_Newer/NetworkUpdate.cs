using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Engine;
using Xenko.UI;
using Xenko.UI.Controls;

namespace VT49_Newer
{
    public class NetworkUpdate : SyncScript
    {
        // Declared public member fields and properties will show in the game studio        
        private TcpClient client = new TcpClient();
        private NetworkStream stream;
        public Entity Ship { get; set; } = null;

		public Entity Camera { get; set; } = null;

		public CameraComponent CameraInsideFront { get; set; } = null;
        public CameraComponent CameraOutsideFront { get; set; } = null;


        
        //private string ClientIP = "127.0.0.1";
        public UIPage ui;
        private EditText TextBox;
        private Button ConnectButton;

        private Button CameraButton_InsideFront;
        private Button CameraButton_OutsideFront;

        public override void Start()
        {
            /*
            IPAddress localAdd = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(localAdd, 4949);
            listener.Start();

            TcpClient client = listener.AcceptTcpClient();
            NetworkStream nwStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];

            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

            //---convert the data received into a string---
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received : " + dataReceived);

            //---write back the text to the client---
            Console.WriteLine("Sending back : " + dataReceived);
            nwStream.Write(buffer, 0, bytesRead);
            client.Close();
            listener.Stop();
            Console.ReadLine();
            */

            //ui = Entity.Components.Get<UIComponent>().Page;                       

            TextBox = ui.RootElement.FindVisualChildOfType<EditText>("IPBox");
            ConnectButton = ui.RootElement.FindVisualChildOfType<Button>("ConnectButton");
            CameraButton_InsideFront = ui.RootElement.FindVisualChildOfType<Button>("CameraInsideFront");
            CameraButton_OutsideFront = ui.RootElement.FindVisualChildOfType<Button>("CameraOutsideFront");
            

            ConnectButton.Click += delegate
			{				
                if (!client.Connected)
                {
                    try
                    {
                        client = new TcpClient(TextBox.Text, 4949);
                        stream = client.GetStream();
                    }
                    catch (ArgumentException e)
                    {
                        DebugText.Print("Could not connect" + e.Message, new Int2(0, 0));
                    }
                }
            };

            CameraButton_InsideFront.Click += delegate
            {
                disableCameras();
                CameraInsideFront.Enabled = true;                
            };

            CameraButton_OutsideFront.Click += delegate
            {
                disableCameras();
                CameraOutsideFront.Enabled = true;                
            };


            void disableCameras()
            {
                CameraInsideFront.Enabled = false;
                CameraOutsideFront.Enabled = false;
            }




        }

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.Escape))
                client.Close();


            if (Input.IsKeyPressed(Keys.F2))
            {
                TextBox.Visibility = Visibility.Hidden;
                ConnectButton.Visibility = Visibility.Hidden;
            }            

            if (Input.IsKeyPressed(Keys.F1))
            {
                TextBox.Visibility = Visibility.Visible;
                ConnectButton.Visibility = Visibility.Visible;
            }

                


            // Do stuff every new frame
            if (client.Connected)
            {
                while (client.Available > 0)
                {
                    byte[] b = new byte[sizeof(float) * 7];
                    stream.Read(b, 0, sizeof(float) * 7);
                    Vector3 pos = new Vector3();
					Quaternion quat = new Quaternion();
					pos.X = BitConverter.ToSingle(b, sizeof(float) * 0);
                    pos.Y = BitConverter.ToSingle(b, sizeof(float) * 1);
                    pos.Z = BitConverter.ToSingle(b, sizeof(float) * 2);

					quat.X = BitConverter.ToSingle(b, sizeof(float) * 3);
					quat.Y = BitConverter.ToSingle(b, sizeof(float) * 4);
					quat.Z = BitConverter.ToSingle(b, sizeof(float) * 5);
					quat.W = BitConverter.ToSingle(b, sizeof(float) * 6);

					//Matrix mat = Matrix.Identity * Matrix.Translation(Ship.Transform.Position) * Matrix.RotationQuaternion(Ship.Transform.Rotation);
					//Matrix mat = Matrix.Translation(Ship.Transform.Position) * Matrix.RotationQuaternion(Ship.Transform.Rotation);

					//mat.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);					
					//Camera.Transform.Position = translation;
					//Camera.Transform.Rotation = rotation;

					//Camera.Transform.Rotation = Ship.Transform.Rotation;
					//Camera.Transform.Position = Ship.Transform.Position;
					//Matrix cameraTransform = Matrix.RotationQuaternion(Ship.Transform.Rotation) + Matrix.Translation(Ship.Transform.Position + new Vector3(0, 1, -5));
					//Camera.Transform.Position = translation;
					//Camera.Transform.Rotation = rotation;

					//Camera.Transform.Position = Vector3.Clamp(
					//	Vector3.Lerp(Camera.Transform.Position, Ship.Transform.Position, 0.125f),
					//	Ship.Transform.Position * 0.01f,
					//	Ship.Transform.Position * 10f
					//	);

					//float glow = Math.Abs(Vector3.Distance(Ship.Transform.Position, pos));					

					//Ship.Components.Get<ModelComponent>().GetMaterial(0).Passes[0].Parameters.Set()

					float distance = Math.Abs(Vector3.Distance(pos, Camera.Transform.Position));
					float smooth = 0.125f;
					if (distance > 2)
					{
						smooth = distance * 0.0625f;
					}

					Camera.Transform.Position = Vector3.Lerp(Camera.Transform.Position, pos, smooth);
					Camera.Transform.Rotation = Quaternion.Lerp(Camera.Transform.Rotation, quat, 0.125f);

					Ship.Transform.Position = pos;
					Ship.Transform.Rotation = quat;

					DebugText.Print(pos.X.ToString(), new Int2(20, 20), Color4.White);
                    DebugText.Print(pos.Y.ToString(), new Int2(20, 40), Color4.White);
                    DebugText.Print(pos.Z.ToString(), new Int2(20, 60), Color4.White);
                }
            }
            
        }
       
    }
}
