using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

namespace CustomHeader
{
	public class MyColumn
	{
		private System.ComponentModel.Container components = null;
		int imageIndex;
		bool ownerDraw;
		bool imageOnRight;
		HorizontalAlignment textAlign;
		int width;
		string text;
		
		public MyColumn()
		{
			InitializeComponent();

			//Default values
			imageIndex = -1;
			ownerDraw = false;
			imageOnRight = false;
			textAlign = HorizontalAlignment.Left;
			width = 60;
			text = "ColumnHeader";
		}		

		#region Public Properties

		public int ImageIndex
		{
			get{return imageIndex;}
			set{imageIndex = value;}
		}

		public bool OwnerDraw
		{
			get{return ownerDraw;}
			set{ownerDraw = value;}
		}

		public bool ImageOnRight
		{
			get{return imageOnRight;}
			set{imageOnRight = value;}
		}

		public string Text
		{
			get{return text;}
			set{text = value;}
		}

		public HorizontalAlignment TextAlign
		{
			get{return textAlign;}
			set{textAlign = value;}
		}

		public int Width
		{
			get{return width;}
			set{width = value;}
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
}
