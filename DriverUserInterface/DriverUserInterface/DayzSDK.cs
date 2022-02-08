using System;
using System.Collections.Generic;
using SharpDX;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Mathematics.Interop;

namespace DriverUserInterface
{
	public enum EntityType
    {
		None,
		Player,
		Zombie,
		Car,
		Animal,
		Loot
    }


	public class DrawingEntity
    {
		public float Distance;
		public Vector3 Position;
		public RawRectangleF DrawRect;
		public RawRectangleF TextRect;
		public string Name;
		public EntityType EntityType;
    }

	public struct _player_t
	{
		public long EntityPtr;
		public long TableEntry;
		public int NetworkID;
	}


	public struct _item_t
	{
		public long ItemPtr;
		public long ItemTable;
	}

	public class Vector2
	{
		public Vector2() 
		{

		}

		public Vector2(float _x, float _y) 
		{
			x = _x; 
		    y = _y;
		}
		~Vector2()
		{

		}

		public float x;
		public float y;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3
	{

		public Vector3(Vector3 value)
		{
			x = value.x;
			y = value.y;
			z = value.z;
		}

		public Vector3(float _x, float _y, float _z) 
		{
			x = _x;
			y = _y;
			z = _z;
		}

		public float x;
		public float z;
		public float y;

		public float Dot(Vector3 v)
		{
			return x * v.x + y * v.y + z * v.z;
		}

		public float Distance(Vector3 v)
		{
			return (float)(Math.Sqrt(Math.Pow(v.x - x, 2.0) + Math.Pow(v.y - y, 2.0) + Math.Pow(v.z - z, 2.0)));
		}

		public static Vector3 operator +(Vector3 x, Vector3 v)
		{
			return new Vector3(x.x + v.x, x.y + v.y, x.z + v.z);
		}

		public static Vector3 operator -(Vector3 x, Vector3 v)
		{
			return new Vector3(x.x - v.x, x.y - v.y, x.z - v.z);
		}
};

//Vector4
public class Vector4
{
	public Vector4()
	{

	}

	Vector4(float _x, float _y, float _z, float _w)
	{

	}
	~Vector4()
	{

	}

		public float x;
		public float y;
		public float z;
		public float w;
};

internal class DayzSDK
    {
		public static long NearEntityTable()
		{
			return Form1._driver.ReadVirtualMemory<long>(Form1.World + 0xEB8);
		}

		public static int NearEntityTableSize()
		{
			return Form1._driver.ReadVirtualMemory<int>(Form1.World + 0xEB8 + 0x08);
		}

		public static long GetEntity(long PlayerList, long SelectedPlayer)
		{
			// Sorted Object
			return Form1._driver.ReadVirtualMemory<long>(PlayerList + SelectedPlayer * 0x8);
		}

		public static string GetEntityTypeName(long Entity)
		{
			// Render Entity Type + Config Name
			return ReadArmaString(Form1._driver.ReadVirtualMemory<long>(Form1._driver.ReadVirtualMemory<long>(Entity + 0x148) + 0xA0));
		}

		public static long FarEntityTable()
		{
			return Form1._driver.ReadVirtualMemory<long>(Form1.World + 0x1000);
		}

		public static int FarEntityTableSize()
		{
			return Form1._driver.ReadVirtualMemory<int>(Form1.World + 0x1000 + 0x08);
		}

		public static long GetCameraOn()
		{
			// Camera On 
			return Form1._driver.ReadVirtualMemory<long>(Form1.World + 0x28A8);
		}

		public static long GetLocalPlayer()
		{
			// Sorted Entity Object
			return Form1._driver.ReadVirtualMemory<long>(Form1._driver.ReadVirtualMemory<long>(Form1.World + 0x28B8) + 0x8) - 0xA8;
		}

		public static long GetLocalPlayerVisualState()
		{
			// Future Visual State
			return Form1._driver.ReadVirtualMemory<long>(GetLocalPlayer() + 0x198);
		}

		public static Vector3 GetCoordinate(long Entity)
		{
			while (true)
			{
				if (Entity == GetLocalPlayer())
				{
					return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetLocalPlayerVisualState() + 0x2C));
				}
				else
				{
					return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(Form1._driver.ReadVirtualMemory<long>(Entity + 0x198) + 0x2C));
				}
			}
		}
		public static long GetCamera()
		{
			while (true)
			{
				return Form1._driver.ReadVirtualMemory<long>(Form1.World + 0x1B8);
			}
		}

		public static Vector3 GetInvertedViewTranslation()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0x2C));
		}
		public static Vector3 GetInvertedViewRight()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0x8));
		}
		public static Vector3 GetInvertedViewUp()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0x14));
		}
		public static Vector3 GetInvertedViewForward()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0x20));
		}

		public static Vector3 GetViewportSize()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0x58));
		}

		public static Vector3 GetProjectionD1()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0xD0));
		}

		public static Vector3 GetProjectionD2()
		{
			return new Vector3(Form1._driver.ReadVirtualMemory<Vector3>(GetCamera() + 0xDC));
		}


		public static int GetSlowEntityTableSize()
		{
			return Form1._driver.ReadVirtualMemory<int>(Form1.World + DayzOffs.SlowEntityTable + 0x08);
		}

		public static float GetDistanceToMe(Vector3 Entity)
		{
			return Entity.Distance(GetCoordinate(GetLocalPlayer()));
		}

		public static bool WorldToScreen(Vector3 Position, ref Vector3 output)
		{
			if (GetCamera() == 0) return false;

			Vector3 temp = Position - GetInvertedViewTranslation();

			var r = GetInvertedViewRight();
			var u = GetInvertedViewUp();
			var f = GetInvertedViewForward();

			float x = temp.Dot(r);
			float y = temp.Dot(u);
			float z = temp.Dot(f);

			if (z < 0.1f)
				return false;

			var t1 = GetProjectionD1();
			var t2 = GetProjectionD2();
			var t3 = GetViewportSize();

			var vsx = GetViewportSize().x;
			var vsy = GetViewportSize().z;
			var D1 = GetProjectionD1().x;
			var D2 = GetProjectionD2().z;
			var res = new Vector3 ( vsx * (1 + (x / D1 / z)),
									vsy * (1 - (y / D2 / z)),
									z);

			output.x = res.x;
			output.y = res.y;
			output.z = res.z;
			return true;
		}

		public static string ReadArmaString(long address)
		{
			int length = Form1._driver.ReadVirtualMemory<int>(address + DayzOffs.OffLength);
			if (length <= 0 || length > 999)
				return "NULL";

			string text = Form1._driver.ReadVirtualMemoryString(address + DayzOffs.OffText, length).ToString();

			return text;
		}
	}
}
