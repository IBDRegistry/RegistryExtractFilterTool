﻿using StripV3Consent.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using static System.Windows.Forms.Control;

namespace StripV3Consent.View
{
    public interface IFileItem<out FileType> where FileType : DataFile
	{
        ControlCollection Controls { get; }
        FlowLayoutPanel control { get; }
        Button CloseButton { get; }
        FileType File { get; }

	}
    public abstract class AbstractFileItem<FileType> : FlowLayoutPanel, IFileItem<FileType> where FileType : DataFile
    {
        private FileType file;

        public PictureBox FileIcon;
        private Button closeButton;

        private const int InteriorSize = 32;

        public Button CloseButton => closeButton;

        public FlowLayoutPanel control => this;


        public FileType File
        {
            get => file;
            set
            {
                file = value;
                DrawContents();
            }
        }

        public AbstractFileItem(FileType file)
        {
            CustomizeControl();
            File = file;
        }

        private void CustomizeControl()
        {
            this.BorderStyle = BorderStyle.Fixed3D;
            this.FlowDirection = FlowDirection.LeftToRight;
            this.WrapContents = false;
            this.AutoSize = true;
        }

       

        public virtual void DrawContents()
        {
            FileIcon = new PictureBox()
            {
                Image = Etier.IconHelper.IconReader.GetFileIcon(File.OutputName, Etier.IconHelper.IconReader.IconSize.Large, false).ToBitmap(),
                Width = InteriorSize,
                Height = InteriorSize
            };
            this.Controls.Add(FileIcon);

            Label FileNameLabel = new Label()
            {
                Text = file.OutputName,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            this.Controls.Add(FileNameLabel);

            closeButton = new Button()
            {
                Text = "X",
                Width = InteriorSize,
                Height = InteriorSize,
            };
            
        }
        public override string ToString()
        {
            return file.Name;
        }
    }
}
