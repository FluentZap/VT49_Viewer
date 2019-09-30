using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Xenko.Core.Mathematics;
using Xenko.Rendering;
using Xenko.Input;
using Xenko.Engine;
using Xenko.UI;
using Xenko.UI.Controls;

namespace VT49_Newer
{


	class PacketDecoder
	{
		byte[] Buffer;
		int index;
		static int Float_S = sizeof(float);
		static int UInt16_S = sizeof(UInt16);

		public PacketDecoder(ref byte[] buffer)
		{
			Buffer = buffer;
		}

		public float Read_Float()
		{
			float value = BitConverter.ToSingle(Buffer, index);
			index += Float_S;
			return value;
		}
		public UInt16 Read_UInt16()
		{
			UInt16 value = BitConverter.ToUInt16(Buffer, index);
			index += UInt16_S;
			return value;
		}
		public byte Read_Byte()
		{
			byte value = Buffer[index];
			index++;
			return value;
		}

		public Vector3 Read_Vector3()
		{
			return new Vector3
				(
					Read_Float(),
					Read_Float(),
					Read_Float()
				);
		}

		public Quaternion Read_Quat()
		{
			return new Quaternion
				(
					Read_Float(),
					Read_Float(),
					Read_Float(),
					Read_Float()
				);
		}
	}

	class SpaceObject
	{		
		public int ObjectType;	
		public Entity entityRef;

		public SpaceObject(ref Entity entity, int type)
		{
			entityRef = entity;
			ObjectType = type;
		}

	}

	public class NetworkUpdate : SyncScript
	{
		const int Float_S = sizeof(float);
		const int Int16_S = sizeof(Int16);
		const int Int32_S = sizeof(Int32);

		int MeshFirstPacketSize =
			sizeof(UInt16) +   //ObjectId
			sizeof(UInt16) +   //ObjectType
			sizeof(float) * 10; //Position Data

		int MeshUpdatePacketSize =
			sizeof(UInt16) +   //ObjectId        
			sizeof(float) * 7; //Position Data

		int ShipPacketSize =
			sizeof(UInt16) +   //ObjectId
			sizeof(float) * 7; //Position Data


		Dictionary<int, SpaceObject> SpaceObjects = new Dictionary<int, SpaceObject>();

		// Declared public member fields and properties will show in the game studio        
		private TcpClient client = new TcpClient();
		private NetworkStream stream;
		public Entity Ship { get; set; } = null;

		public Entity Camera { get; set; } = null;

		//private string ClientIP = "127.0.0.1";
		public UIPage ui;
		private EditText TextBox;
		private Button ConnectButton;

		private Button CameraButton_InsideFront;
		private Button CameraButton_OutsideFront;




		public void SetModelDisplay()
		{
			var modelComponent = Ship.Get<ModelComponent>();
			Dictionary<string, int> Nodes = new Dictionary<string, int>();
			for (int nodeIndex = 0; nodeIndex < modelComponent.Model.Skeleton.Nodes.Length; nodeIndex++)
			{
				var nodeName = modelComponent.Skeleton.Nodes[nodeIndex].Name;
				Nodes.Add(nodeName, nodeIndex);
			}

			//modelComponent.Skeleton.NodeTransformations[Nodes["Engines"]].Transform.Scale = new Vector3(0, 0, 0);
			//modelComponent.Skeleton.NodeTransformations[Nodes["Engines_Glow"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Engines_Type2"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Engines_Type2_Glow"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Radar_Type1"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Radar_Type2"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Bosters_Type1"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Bosters_Type1_Glows"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Boosters_Type2"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Boosters_Type2_Glow"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret_Guns_R"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret_Guns_L"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret_Guns_R"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret_Guns_L"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret2_Gun_R"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret2_Gun_L"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret2_Gun_R"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret2_Gun_L"]].Transform.Scale = new Vector3(0, 0, 0);


			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret_Mount"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Bottom_Turret2"]].Transform.Scale = new Vector3(0, 0, 0);

			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret_Mount"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret"]].Transform.Scale = new Vector3(0, 0, 0);
			modelComponent.Skeleton.NodeTransformations[Nodes["Top_Turret2"]].Transform.Scale = new Vector3(0, 0, 0);
		}


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

			//Entity asteroid_1 = new Entity(new Vector3(10, 10, 10), "Asteroid_1");
			//ModelComponent model = new ModelComponent()
			//asteroid_1.Components.Add()

			//Scene.Entities.Add();



			//// Load a model (replace URL with valid URL)
			//var model = Content.Load<Model>("SpaceObjects/Asteroid_Type1");

			//// Create a new entity to add to the scene
			//Entity entity = new Entity(new Vector3(15, 0, 15), "Asteroid_1") { new ModelComponent { Model = model } };
			//entity.Transform.Scale = new Vector3(10, 10, 10);

			//// Add a new entity to the scene
			//SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

			SetModelDisplay();

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

			//CameraButton_InsideFront.Click += delegate
			//{
			//    disableCameras();
			//    CameraInsideFront.Enabled = true;                
			//};

			//CameraButton_OutsideFront.Click += delegate
			//{
			//    disableCameras();
			//    CameraOutsideFront.Enabled = true;                
			//};


			//void disableCameras()
			//{
			//    CameraInsideFront.Enabled = false;
			//    CameraOutsideFront.Enabled = false;
			//}




		}

		public override void Update()
		{			
			if (Input.IsKeyPressed(Keys.Escape))
			{
				client.Close();
			}

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
					int Header = stream.ReadByte();

					switch (Header)
					{
						case 0:
							LoadInitialSystem();							
							break;
						case 1:
							UpdateShips();
							break;
					}
				}
			}

		}

		UInt16 ReadInt16()
		{
			byte[] b = new byte[Int16_S];
			stream.Read(b, 0, Int16_S);
			return BitConverter.ToUInt16(b, 0);
		}


		void LoadInitialSystem()
		{
			UInt16 count = ReadInt16();
			int packetSize = count * MeshFirstPacketSize;
			byte[] b = new byte[packetSize];

			stream.Read(b, 0, b.Length);

			PacketDecoder decoder = new PacketDecoder(ref b);
			//List<SpaceObject> spaceObjects = new List<SpaceObject>();
			foreach (var item in SpaceObjects)
			{
				SceneSystem.SceneInstance.RootScene.Entities.Remove(item.Value.entityRef);
			}
			SpaceObjects.Clear();

			for (ushort i = 0; i < count; i++)
			{				
				int id = decoder.Read_UInt16();
				int type = decoder.Read_UInt16();
				Vector3 location = decoder.Read_Vector3();
				Vector3 scale = decoder.Read_Vector3();
				Quaternion rotation = decoder.Read_Quat();

				Entity entitieRef = AddObject(type, id, location, scale, rotation);				
				SpaceObjects.Add(id, new SpaceObject(ref entitieRef, type));
			}
		}
		


		Entity AddObject(int objectType, int Id, Vector3 locaiton, Vector3 scale, Quaternion rotation)
		{
			Model model;
			switch (objectType)
			{
				case 2:
					model = Content.Load<Model>("SpaceObjects/Asteroid_Type1");
					break;
				case 3:
					model = Content.Load<Model>("SpaceObjects/Asteroid_Type2");
					break;
				case 4:
					model = Content.Load<Model>("SpaceObjects/Asteroid_Type3");
					break;

				default:
					model = Content.Load<Model>("SpaceObjects/Asteroid_Type1");
					break;
			}

			

			// Create a new entity to add to the scene
			Entity entity = new Entity( locaiton, $"Object_{Id}") { new ModelComponent { Model = model } };
			entity.Transform.Rotation = rotation;
			entity.Transform.Scale = scale;

			// Add a new entity to the scene
			SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

			return entity;
		}		



		private void UpdateShips()
		{
			UInt16 count = ReadInt16();
			int size = ShipPacketSize + (count * MeshUpdatePacketSize);						
			byte[] b = new byte[size];
			stream.Read(b, 0, size);
			PacketDecoder decoder = new PacketDecoder(ref b);
			//Ship
			int shipId = decoder.Read_UInt16();
			Vector3 pos = decoder.Read_Vector3();
			Quaternion quat = decoder.Read_Quat();
			
			for (int i = 0; i < count; i++)
			{
				int id = decoder.Read_UInt16();
				if (SpaceObjects.ContainsKey(id))
				{
					SpaceObject spaceObject = SpaceObjects[id];
					spaceObject.entityRef.Transform.Position = decoder.Read_Vector3();
					spaceObject.entityRef.Transform.Rotation = decoder.Read_Quat();
				}
			}

			


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

			float PositionDistance = Math.Abs(Vector3.Distance(pos, Camera.Transform.Position));
			float PositionSmooth = 0.2f;
			//> 2 and < 16
			if (PositionDistance > 2)
			{
				if (PositionDistance <= 10)
				{
					PositionSmooth = PositionDistance * 0.1f;
				}
				else
				{
					PositionSmooth = 1;
				}
			}

			//float RotationDistance = Math.Abs(Vector3.Distance(pos, Camera.Transform.Position));
			float RotationDistance = Math.Abs(Vector3.Distance(quat.Axis, Camera.Transform.Rotation.Axis));
			float RotationSmooth = 0.1f;
			//> 2 and < 16
			if (RotationDistance > 1)
			{
				if (RotationDistance <= 10)
				{
					RotationSmooth = RotationDistance * 0.1f;
				}
				else
				{
					RotationSmooth = 1;
				}
			}

			Camera.Transform.Position = Vector3.Lerp(Camera.Transform.Position, pos, PositionSmooth);
			Camera.Transform.Rotation = Quaternion.Slerp(Camera.Transform.Rotation, quat, 0.125f);

			Ship.Transform.Position = Vector3.Lerp(Ship.Transform.Position, pos, 0.99f);
			Ship.Transform.Rotation = Quaternion.Slerp(Ship.Transform.Rotation, quat, 0.99f);

			//DebugText.Print(pos.X.ToString(), new Int2(20, 20), Color4.White);
			//DebugText.Print(pos.Y.ToString(), new Int2(20, 40), Color4.White);
			//DebugText.Print(pos.Z.ToString(), new Int2(20, 60), Color4.White);

			if (SceneSystem.SceneInstance.RootScene.Entities[40] != null)
			{
				DebugText.Print(SceneSystem.SceneInstance.RootScene.Entities[40].Transform.Position.X.ToString(), new Int2(20, 20), Color4.White);
			}			



			//DebugText.Print(Camera.Transform.Position.X.ToString(), new Int2(20, 20), Color4.White);
			//DebugText.Print(Camera.Transform.Position.Y.ToString(), new Int2(20, 40), Color4.White);
			//DebugText.Print(Camera.Transform.Position.Z.ToString(), new Int2(20, 60), Color4.White);

			//DebugText.Print(quat.Angle.ToString(), new Int2(20, 90), Color4.White);
		}

	}
}
