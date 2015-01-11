using System;
using System.IO;
using System.Collections;
using System.Text;

namespace Assistant
{
	[Flags]
	public enum Direction : byte
	{
		North = 0x0,
		Right = 0x1,
		East = 0x2,
		Down = 0x3,
		South = 0x4,
		Left = 0x5,
		West = 0x6,
		Up = 0x7,
		Mask = 0x7,
		Running = 0x80,
		ValueMask = 0x87
	}

	public class Mobile : UOEntity
	{
		private ushort m_Body;
		private Direction m_Direction;
		private string m_Name;

		private byte m_Notoriety;

		private bool m_Visible;
		private bool m_Female;
		private bool m_Poisoned;
		private bool m_Blessed;
		private bool m_Warmode;

		private ushort m_HitsMax, m_Hits;

		private ArrayList m_Items;

		private byte m_Map;

		public override void SaveState( BinaryWriter writer )
		{
			base.SaveState (writer);

			writer.Write( m_Body );
			writer.Write( (byte)m_Direction );
			writer.Write( m_Name == null ? "" : m_Name );
			writer.Write( m_Notoriety );
			writer.Write( (byte)GetPacketFlags() );
			writer.Write( m_HitsMax );
			writer.Write( m_Hits );
			writer.Write( m_Map );
			
			writer.Write( (int)m_Items.Count );
			for(int i=0;i<m_Items.Count;i++)
				writer.Write( (uint)(((Item)m_Items[i]).Serial) );
			//writer.Write( (int)0 );
		}

		public Mobile( BinaryReader reader, int version ) : base( reader, version )
		{
			m_Body = reader.ReadUInt16();
			m_Direction = (Direction)reader.ReadByte();
			m_Name = reader.ReadString();
			m_Notoriety = reader.ReadByte();
			ProcessPacketFlags( reader.ReadByte() );
			m_HitsMax = reader.ReadUInt16();
			m_Hits = reader.ReadUInt16();
			m_Map = reader.ReadByte();

			int count = reader.ReadInt32();
			m_Items = new ArrayList( count );
			for(int i=0;i<count;i++)
				m_Items.Add( (Serial)reader.ReadUInt32() );
		}

		public override void AfterLoad()
		{	
			for(int i=0;i<m_Items.Count;i++)
			{
				if ( m_Items[i] is Serial )
				{
					m_Items[i] = World.FindItem( (Serial)m_Items[i] );

					if ( m_Items[i] == null )
					{
						m_Items.RemoveAt( i );
						i--;
					}
				}
			}
		}

		public Mobile( Serial serial ) : base( serial )
		{
			m_Items = new ArrayList();
			m_Map = World.Player == null ? (byte)0 : World.Player.Map;
			m_Visible = true;
		}

		public string Name
		{
			get
			{ 
				if ( m_Name == null )
					return "";
				else
					return m_Name;
			}
			set
			{ 
				if ( value != null )
				{
					string trim = value.Trim();
					if ( trim.Length > 0 )
						m_Name = trim;
				}
			}
		}

		public ushort Body
		{
			get{ return m_Body; }
			set{ m_Body = value; }
		}

		public Direction Direction
		{
			get{ return m_Direction; }
			set{ m_Direction = value; }
		}

		public bool Visible
		{
			get{ return m_Visible; }
			set{ m_Visible = value; }
		}

		public bool Poisoned
		{
			get{ return m_Poisoned; }
			set{ m_Poisoned = value; }
		}

		public bool Blessed
		{
			get{ return m_Blessed; }
			set{ m_Blessed = value; }
		}

		public bool Warmode
		{
			get{ return m_Warmode; }
			set{ m_Warmode = value; }
		}

		public bool Female
		{
			get{ return m_Female; }
			set{ m_Female = value; }
		}

		public byte Notoriety
		{
			get{ return m_Notoriety;  }
			set
			{ 
				if ( value != Notoriety )
				{
					OnNotoChange( m_Notoriety, value );
					m_Notoriety = value; 
				}	
			}
		}

		protected virtual void OnNotoChange( byte old, byte cur )
		{
		}

		// grey, blue, green, 'canbeattacked'
		private static uint[] m_NotoHues = new uint[8] 
		{ 
			// hue color #30
			0x000000, // black	    unused 0
			0x30d0e0, // blue		0x0059 1 
			0x60e000, // green		0x003F 2
			0x9090b2, // greyish	0x03b2 3
			0x909090, // grey		   "   4
			0xd88038, // orange		0x0090 5
			0xb01000, // red		0x0022 6
			0xe0e000, // yellow		0x0035 7
		};

		public uint GetNotorietyColor()
		{
			if ( m_Notoriety < 0 || m_Notoriety >= m_NotoHues.Length )
				return m_NotoHues[0];
			else
				return m_NotoHues[m_Notoriety];
		}

		public byte GetStatusCode()
		{
			if ( m_Poisoned )
				return 1;
			else
				return 0;
		}

		public ushort HitsMax
		{
			get{ return m_HitsMax; }
			set{ m_HitsMax = value; }
		}

		public ushort Hits
		{
			get{ return m_Hits; }
			set{ m_Hits = value; }
		}

		public byte Map
		{
			get{ return m_Map; }
			set
			{ 
				if ( m_Map != value )
				{
					OnMapChange( m_Map, value );
					m_Map = value; 
				}
			}
		}

		public virtual void OnMapChange( byte old, byte cur )
		{
		}
		
		public void AddItem( Item item )
		{
			m_Items.Add( item );
		}

		public void RemoveItem( Item item )
		{
			m_Items.Remove( item );
		}

		public override void Remove()
		{
			ArrayList rem = new ArrayList( m_Items );
			m_Items.Clear();
			for (int i=0;i<rem.Count;i++)
				((Item)rem[i]).Remove();

			World.RemoveMobile( this );
			base.Remove();
		}

		public Item GetItemOnLayer( byte layer )
		{
			for(int i=0;i<m_Items.Count;i++)
			{
				Item item = (Item)m_Items[i];
				if ( item.Layer == layer )
					return item;
			}
			return null;
		}

		public Item Backpack 
		{
			get
			{
				return GetItemOnLayer( 0x15 );
			}
		}

		public Item FindItemByID( ItemID id )
		{
			for (int i=0;i<Contains.Count;i++)
			{
				Item item = (Item)Contains[i];
				if ( item.ItemID == id )
					return item;
			}
			return null;
		}
		
		public int GetPacketFlags()
		{
			int flags = 0x0;

			if ( m_Female )
				flags |= 0x02;

			if ( m_Poisoned )
				flags |= 0x04;

			if ( m_Blessed )
				flags |= 0x08;

			if ( m_Warmode )
				flags |= 0x40;

			if ( !m_Visible )
				flags |= 0x80;

			return flags;
		}

		public void ProcessPacketFlags( byte flags )
		{
			m_Female = (flags&0x02) != 0;
			m_Poisoned = (flags&0x04) != 0;
			m_Blessed = (flags&0x08) != 0;
			m_Warmode = (flags&0x40) != 0;
			m_Visible = (flags&0x80) == 0;
		}

		public ArrayList Contains{ get{ return m_Items; } }
	}
}

