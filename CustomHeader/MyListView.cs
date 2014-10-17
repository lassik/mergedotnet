using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CustomHeader
{
	//The delegate for the HeaderEventArgs class
	public delegate void HeaderEventHandler(object sender, HeaderEventArgs e);
	//The delegate for drawing the entire header
	public delegate void DrawHeaderEventHandler(DrawHeaderEventArgs e);

	public class MyListView : ListView
	{
		//The event for drawing columns
		public event DrawItemEventHandler DrawColumn;
		//The event for handling the entire border drawing of the header
		public event DrawHeaderEventHandler DrawHeader;

		//Event handlers associated with the header control
		public event HeaderEventHandler BeginDragHeaderDivider;
		public event HeaderEventHandler DragHeaderDivider;
		public event HeaderEventHandler EndDragHeaderDivider;

		private System.ComponentModel.Container components = null;

		MyHeaderCollection myColumns;		
		ImageList headerImages;
		HeaderControl header;
		bool ownerDrawHeader;
		int increaseHeaderHeight;
		Color headerBackColor;
		int headerHeight;
		bool defaultCustomDraw;
		
		public MyListView()
		{
			InitializeComponent();

			myColumns = new MyHeaderCollection(); 
			ownerDrawHeader = false;			
			headerBackColor = SystemColors.Control;
			increaseHeaderHeight = 0;
			defaultCustomDraw = false;
			this.CheckBoxes = true;
			
			InsertColumns();
		}

		#region Overriden methods

		protected override void OnHandleCreated(EventArgs e)
		{
			//Create a new HeaderControl object
			header = new HeaderControl(this);
			if(header.Handle != IntPtr.Zero)
			{
				if(headerImages != null)//If we have a valid header handle and a valid ImageList for it
					//send a message HDM_SETIMAGELIST
					Win32.SendMessage(header.Handle,0x1200+8,IntPtr.Zero,headerImages.Handle);
				//Insert all the columns in Columns collection
				if(this.Columns.Count > 0)
					InsertColumns();				
			}
			base.OnHandleCreated(e);
		}

		protected override void WndProc(ref Message m)
		{
			Win32.NMHEADER nm;
			switch(m.Msg)
			{
				case 0x004E://WM_NOTIFY
					base.WndProc(ref m);
					Win32.NMHDR nmhdr = (Win32.NMHDR)m.GetLParam(typeof(Win32.NMHDR));
				switch(nmhdr.code)
				{
					case (0-300-26)://HDN_BEGINTRACK
						nm=(Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
						if(BeginDragHeaderDivider != null)
							BeginDragHeaderDivider(this.Columns[nm.iItem], 
								new HeaderEventArgs(nm.iItem, nm.iButton));
						break;
					case (0-300-20)://HDN_ITEMCHANGING
						nm=(Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
						//Adjust the column width
						Win32.RECT rect = new Win32.RECT();
						//HDM_GETITEMRECT
						Win32.SendMessage(header.Handle, 0x1200+7, nm.iItem, ref rect);
						//Get the item height which is actually header's height
						this.headerHeight = rect.bottom-rect.top;
						this.Columns[nm.iItem].Width = rect.right - rect.left;
						if(DragHeaderDivider != null)
							DragHeaderDivider(this.Columns[nm.iItem],
								new HeaderEventArgs(nm.iItem, nm.iButton));
						break;
					case (0-300-27)://HDN_ENDTRACK
						nm=(Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
						if(EndDragHeaderDivider != null)
							EndDragHeaderDivider(this.Columns[nm.iItem],
								new HeaderEventArgs(nm.iItem, nm.iButton));
						break;
				}
					break;
				/*case (int)Win32.OCM.OCM_NOTIFY://Reflected WM_NOTIFY message
					Win32.NMHDR nmh = (Win32.NMHDR)m.GetLParam(typeof(Win32.NMHDR));
					switch(nmh.code)
					{
						case (int)Win32.NM.NM_CUSTOMDRAW:
							Win32.NMCUSTOMDRAW nmcd = 
									(Win32.NMCUSTOMDRAW)m.GetLParam(typeof(Win32.NMCUSTOMDRAW));
							if(nmcd.hdr.hwndFrom != this.Handle)
								break;
						switch(nmcd.dwDrawStage)
						{
							case (int)Win32.CDDS.CDDS_PREPAINT:
								m.Result = (IntPtr)Win32.CDRF.CDRF_NOTIFYITEMDRAW;
								break;
							case (int)Win32.CDDS.CDDS_ITEMPREPAINT:
								m.Result = (IntPtr)Win32.CDRF.CDRF_NOTIFYITEMDRAW;
								break;
							case (int)(Win32.CDDS.CDDS_SUBITEM|Win32.CDDS.CDDS_ITEMPREPAINT):
								Win32.NMLVCUSTOMDRAW nmlv = (Win32.NMLVCUSTOMDRAW)
									m.GetLParam(typeof(Win32.NMLVCUSTOMDRAW));
								//Color c = Color.Brown;
								/*if(this.SelectedIndices.Count > 0)
								{
									if(nmlv.nmcd.dwItemSpec == this.SelectedIndices[0])
									{
										nmlv.clrTextBk = Win32.RGB(c.R,c.G,c.B);
										//this.Items.Add(nmlv.nmcd.uItemState.ToString());
									}
								}
								//Marshal.StructureToPtr(nmlv,m.LParam,true);
								m.Result = (IntPtr)Win32.CDRF.CDRF_NOTIFYPOSTPAINT;
								break;
							case (int)(Win32.CDDS.CDDS_SUBITEM|Win32.CDDS.CDDS_ITEMPOSTPAINT):
								Win32.NMLVCUSTOMDRAW nmlv1 = (Win32.NMLVCUSTOMDRAW)
									m.GetLParam(typeof(Win32.NMLVCUSTOMDRAW));
								Color c = Color.Brown;
								Graphics g = Graphics.FromHdc(nmlv1.nmcd.hdc);
								if(this.SelectedIndices.Count > 0)
								{
									if(nmlv1.nmcd.dwItemSpec == this.SelectedIndices[0])
									{
										//nmlv.clrTextBk = Win32.RGB(c.R,c.G,c.B);
										//this.Items.Add(nmlv.nmcd.uItemState.ToString());
										Rectangle r = new Rectangle(nmlv1.nmcd.rc.left,nmlv1.nmcd.rc.top,
											nmlv1.nmcd.rc.right-nmlv1.nmcd.rc.left,
											nmlv1.nmcd.rc.bottom-nmlv1.nmcd.rc.top);
										g.FillRectangle(Brushes.Brown,r);
									}
								}
								//Marshal.StructureToPtr(nmlv1,m.LParam,true);
								//m.Result = (IntPtr)Win32.CDRF.CDRF_DODEFAULT;
								break;
						}
							break;
					}
					break;*/
				case 0x002B://WM_DRAWITEM
					//Get the DRAWITEMSTRUCT from the LParam of the message
					Win32.DRAWITEMSTRUCT dis = (Win32.DRAWITEMSTRUCT)Marshal.PtrToStructure(
						m.LParam,typeof(Win32.DRAWITEMSTRUCT));
					//Check if this message comes from the header
					if(dis.ctrlType == 100)//ODT_HEADER - it do comes from the header
					{
						//Get the graphics from the hdc field of the DRAWITEMSTRUCT
						Graphics g = Graphics.FromHdc(dis.hdc);
						//Create a rectangle from the RECT struct
						Rectangle r = new Rectangle(dis.rcItem.left, dis.rcItem.top, dis.rcItem.right -
							dis.rcItem.left, dis.rcItem.bottom - dis.rcItem.top);

                        //Create new DrawItemState in its default state					
						DrawItemState d = DrawItemState.Default;
						//Set the correct state for drawing
						if(dis.itemState == 0x0001)
							d = DrawItemState.Selected;
						//Create the DrawItemEventArgs object
						DrawItemEventArgs e = new DrawItemEventArgs(g,this.Font,r,dis.itemID,d);
						//If we have a handler attached call it and we don't want the default drawing
						if(DrawColumn != null && !defaultCustomDraw)
							DrawColumn(this.Columns[dis.itemID], e);
						else if(defaultCustomDraw)
							DoMyCustomHeaderDraw(this.Columns[dis.itemID],e);
						//Release the graphics object					
						g.Dispose();					
					}
					break;
				case 0x0002://WM_DESTROY
					//Release the handle associated with the header control window
					header.ReleaseHandle();
					base.WndProc(ref m);
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		#endregion

		#region Drawing methods

		void DrawHeaderBorder(DrawHeaderEventArgs e)
		{
			Graphics g = e.Graphics;
			Rectangle r = new Rectangle(e.Bounds.Left,e.Bounds.Top,e.Bounds.Width,e.Bounds.Height);
			if(r.Left == 0)
				g.DrawLine(SystemPens.ControlLightLight,r.Left,r.Bottom,r.Left,r.Top);
			if(r.Top == 0)
				g.DrawLine(SystemPens.ControlLightLight,r.Left,r.Top,r.Right,r.Top);
			if(r.Bottom == e.HeaderHeight)
				g.DrawLine(SystemPens.ControlDark,r.Left,r.Bottom-1,r.Right,r.Bottom-1);
		}

		void DoMyCustomHeaderDraw(object sender, DrawItemEventArgs e)
		{
			MyColumn m = sender as MyColumn;			
			Graphics g = e.Graphics;
			//Get the text width
			SizeF szf = g.MeasureString(m.Text, this.Font);
			int textWidth = (int)szf.Width+10;
			Image image = null;
			
			Rectangle r = e.Bounds;
			int leftOffset = 4;
			int rightOffset = 4;
			
			StringFormat s = new StringFormat();
			s.FormatFlags = StringFormatFlags.NoWrap;
			s.Trimming = StringTrimming.EllipsisCharacter;
			switch(m.TextAlign)
			{
				case HorizontalAlignment.Left:
					s.Alignment = StringAlignment.Near;
					break;
				case HorizontalAlignment.Center:
					s.Alignment = StringAlignment.Center;
					break;
				case HorizontalAlignment.Right:
					s.Alignment = StringAlignment.Far;
					break;
			}
			s.LineAlignment = StringAlignment.Center;
			//Adjust the proper text bounds and get the correct image(if any)
			if(m.ImageIndex != -1 && headerImages != null)
			{
				if(m.ImageIndex + 1 > headerImages.Images.Count)
					image = null;
				else
				{
					if(m.ImageOnRight)
						rightOffset += 20;
					else
						leftOffset += 20;
					image = new Bitmap(headerImages.Images[m.ImageIndex],16,16);
				}
			}
			if(textWidth+leftOffset+rightOffset > r.Width)
				textWidth = r.Width - leftOffset - rightOffset;
			
			Rectangle text = new Rectangle(r.Left+leftOffset, r.Top, 
				textWidth, r.Height);
			Rectangle img = Rectangle.Empty;
			if(image != null)
			{
				if(m.ImageOnRight)
					img = new Rectangle(text.Right+4,(r.Height-16)/2,16,16);
				else
					img = new Rectangle(r.Left+2,(r.Height-16)/2,16,16);
			}

            if(!this.FullyCustomHeader)
				g.FillRectangle(new SolidBrush(this.headerBackColor),r);
			//This occurs when column is pressed
			if((e.State & DrawItemState.Selected)!=0)
			{
				g.DrawLine(SystemPens.ControlDark,r.Right-1, r.Bottom-1, r.Right-1, r.Top);
				g.DrawLine(SystemPens.ControlLightLight,r.Right, r.Bottom-1, r.Right, r.Top);

				g.DrawLine(SystemPens.ControlDark,r.Left+2, r.Bottom-3, r.Left+2, r.Top+2);
				g.DrawLine(SystemPens.ControlDark,r.Left+2, r.Top+2, r.Right-3, r.Top+2);
				g.DrawLine(SystemPens.ControlLightLight,r.Right-3, r.Top+2, r.Right-3, r.Bottom-3);
				g.DrawLine(SystemPens.ControlLightLight,r.Right-3, r.Bottom-3, r.Left+2, r.Bottom-3);

				if(image != null)
				{
					img.Offset(1,1);
					g.DrawImage(image,img);
					img.Offset(-1,-1);
				}

				text.Offset(1,1);
				g.DrawString(m.Text,e.Font,SystemBrushes.WindowText,text,s);
				text.Offset(-1,-1);
			}
			//Default state
			else
			{
				g.DrawLine(new Pen(this.headerBackColor),r.Right-2, r.Bottom, r.Right-2, r.Top);
				g.DrawLine(new Pen(this.headerBackColor),r.Right-1, r.Bottom, r.Right-1, r.Top);

				g.DrawLine(SystemPens.ControlDark,r.Right-1, r.Bottom-1, r.Right-1, r.Top);
				g.DrawLine(SystemPens.ControlLightLight,r.Right, r.Bottom-1, r.Right, r.Top);
				
				g.DrawString(m.Text,e.Font,SystemBrushes.WindowText,text,s);
				if(image != null)
					g.DrawImage(image,img);
				
			}

		}

		#endregion

		#region InsertColumns Method

		void InsertColumns()
		{
			int counter = 0;
			foreach(MyColumn m in myColumns)
			{
				Win32.LVCOLUMN lvc = new Win32.LVCOLUMN();
				lvc.mask = 0x0001|0x0008|0x0002|0x0004;//LVCF_FMT|LVCF_SUBITEM|LVCF_WIDTH|LVCF_TEXT
				lvc.cx = m.Width;
				lvc.subItem = counter;
				lvc.text = m.Text;
				switch(m.TextAlign)
				{
					case HorizontalAlignment.Left:
						lvc.fmt = 0x0000;
						break;
					case HorizontalAlignment.Center:
						lvc.fmt = 0x0002;
						break;
					case HorizontalAlignment.Right:
						lvc.fmt = 0x0001;
						break;
				}
				if(headerImages != null && m.ImageIndex != -1)
				{
					lvc.mask |= 0x0010;//LVCF_IMAGE
					lvc.iImage = m.ImageIndex;
					lvc.fmt |= 0x0800;//LVCFMT_IMAGE
					if(m.ImageOnRight)
						lvc.fmt |= 0x1000;
				}
				//Send message LVN_INSERTCOLUMN
				Win32.SendMessage(this.Handle,0x1000+97,counter,ref lvc);
				//Check if column is set to owner-draw
				//If so - send message HDM_SETITEM with HDF_OWNERDRAW flag set
				if(m.OwnerDraw)
				{
					Win32.HDITEM hdi = new Win32.HDITEM();
					hdi.mask = (int)Win32.HDI.HDI_FORMAT;
					hdi.fmt = (int)Win32.HDF.HDF_OWNERDRAW;
					Win32.SendMessage(header.Handle,0x1200+12,counter,ref hdi);
				}
				counter++;
			}
		}

		#endregion

		#region Public properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public new MyHeaderCollection Columns
		{
			get{return myColumns;}
		}

		public ImageList HeaderImageList
		{
			get{return headerImages;}
			set{headerImages = value;}
		}

		public bool FullyCustomHeader
		{
			get{return ownerDrawHeader;}
			set{ownerDrawHeader = value;}
		}

		public IntPtr HeaderHandle
		{
			get{return header.Handle;}
		}

		public int IncreaseHeaderHeight
		{
			get{return increaseHeaderHeight;}
			set
			{
				increaseHeaderHeight = value;
			}
		}

		public int HeaderHeight
		{
			get{return headerHeight;}
		}

		public bool DefaultCustomDraw
		{
			get{return defaultCustomDraw;}
			set{defaultCustomDraw = value;}
		}

		#endregion

		#region HeaderControl class

		internal class HeaderControl : NativeWindow
		{
			MyListView parent;
			bool mouseDown;
			public HeaderControl(MyListView m)
			{
				parent = m;
				//Get the header control handle
				IntPtr header = Win32.SendMessage(parent.Handle, (0x1000+31), IntPtr.Zero, IntPtr.Zero);
				this.AssignHandle(header);				
			}

			#region Overriden WndProc

			protected override void WndProc(ref Message m)
			{
				switch(m.Msg)
				{
					case 0x000F://WM_PAINT
						if(parent.FullyCustomHeader)
						{
							Win32.RECT update = new Win32.RECT();
							if(Win32.GetUpdateRect(m.HWnd,ref update, false)==0)
								break;
							//Fill the paintstruct
							Win32.PAINTSTRUCT ps = new Win32.PAINTSTRUCT();
							IntPtr hdc = Win32.BeginPaint(m.HWnd, ref ps);
							//Create graphics object from the hdc
							Graphics g = Graphics.FromHdc(hdc);
							//Get the non-item rectangle
							int left = 0;
							Win32.RECT itemRect = new Win32.RECT();
							for(int i=0; i<parent.Columns.Count; i++)
							{								
								//HDM_GETITEMRECT
								Win32.SendMessage(m.HWnd, 0x1200+7, i, ref itemRect);
								left += itemRect.right-itemRect.left;								
							}
							parent.headerHeight = itemRect.bottom-itemRect.top;
							if(left >= ps.rcPaint.left)
								left = ps.rcPaint.left;

                            Rectangle r = new Rectangle(left, ps.rcPaint.top, 
								ps.rcPaint.right-left, ps.rcPaint.bottom-ps.rcPaint.top);
							Rectangle r1 = new Rectangle(ps.rcPaint.left, ps.rcPaint.top, 
								ps.rcPaint.right-left, ps.rcPaint.bottom-ps.rcPaint.top);

							g.FillRectangle(new SolidBrush(parent.headerBackColor),r);

							//If we have a valid event handler - call it
							if(parent.DrawHeader != null && !parent.DefaultCustomDraw)
								parent.DrawHeader(new DrawHeaderEventArgs(g,r,
									itemRect.bottom-itemRect.top));
							else
								parent.DrawHeaderBorder(new DrawHeaderEventArgs(g,r,
									itemRect.bottom-itemRect.top));
							//Now we have to check if we have owner-draw columns and fill
							//the DRAWITEMSTRUCT appropriately
							int counter = 0;
							foreach(MyColumn mm in parent.Columns)
							{
								if(mm.OwnerDraw)
								{
									Win32.DRAWITEMSTRUCT dis = new Win32.DRAWITEMSTRUCT();
									dis.ctrlType = 100;//ODT_HEADER
									dis.hwnd = m.HWnd;
									dis.hdc = hdc;
									dis.itemAction = 0x0001;//ODA_DRAWENTIRE
									dis.itemID = counter;
									//Must find if some item is pressed
									Win32.HDHITTESTINFO hi = new Win32.HDHITTESTINFO();
									hi.pt.X = parent.PointToClient(MousePosition).X;
									hi.pt.Y = parent.PointToClient(MousePosition).Y;
									int hotItem = Win32.SendMessage(m.HWnd, 0x1200+6, 0, ref hi);
									//If clicked on a divider - we don't have hot item
									if(hi.flags == 0x0004 || hotItem != counter)
										hotItem = -1;
									if(hotItem != -1 && mouseDown)
										dis.itemState = 0x0001;//ODS_SELECTED
									else
										dis.itemState = 0x0020;
									//HDM_GETITEMRECT
									Win32.SendMessage(m.HWnd, 0x1200+7, counter, ref itemRect);
									dis.rcItem = itemRect;
									//Send message WM_DRAWITEM
									Win32.SendMessage(parent.Handle,0x002B,0,ref dis);
								}
								counter++;
							}
							Win32.EndPaint(m.HWnd, ref ps);
							
						}
						else
							base.WndProc(ref m);						
						break;
					case 0x0014://WM_ERASEBKGND
						//We don't need to do anything here in order to reduce flicker
						if(parent.FullyCustomHeader)
							break;						
						else
							base.WndProc(ref m);
						break;
				case 0x0201://WM_LBUTTONDOWN
						mouseDown = true;
						base.WndProc(ref m);
						break;
				case 0x0202://WM_LBUTTONUP
						mouseDown = false;
						base.WndProc(ref m);
						break;
				case 0x1200+5://HDM_LAYOUT
						base.WndProc(ref m);
						break;
				case 0x0030://WM_SETFONT						
						if(parent.IncreaseHeaderHeight > 0)
						{
							System.Drawing.Font f = new System.Drawing.Font(parent.Font.Name,
								parent.Font.SizeInPoints + parent.IncreaseHeaderHeight);
							m.WParam = f.ToHfont();
						}						
                        base.WndProc(ref m);						
						break;
					default:
						base.WndProc(ref m);
						break;
				}
			}

			#endregion
		}

		#endregion		

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}

	#region HeaderEventArgs class

	public class HeaderEventArgs : EventArgs
	{
		int columnIndex;
		int mouseButton;
		public HeaderEventArgs(int index, int button)
		{
			columnIndex = index;
			mouseButton = button;
		}
		public int ColumnIndex
		{
			get{return columnIndex;}
		}
		public int MouseButton
		{
			get{return mouseButton;}
		}
	}

		#endregion

	#region DrawHeaderEventArgs class

	public class DrawHeaderEventArgs : EventArgs
	{
		Graphics graphics;
		Rectangle bounds;
		int height;
		public DrawHeaderEventArgs(Graphics dc, Rectangle rect, int h)
		{
			graphics = dc;
			bounds = rect;
			height = h;
		}
		public Graphics Graphics
		{
			get{return graphics;}
		}		
		public Rectangle Bounds
		{
			get{return bounds;}
		}
		public int HeaderHeight
		{
			get{return height;}
		}
	}

	#endregion
}
