﻿using Myra.Graphics2D.UI;

namespace TestMod;

public class TestUiWindow : Window
{
    public TestUiWindow()
    {
        var label1 = new Label
        {
            Text = "Positioned Text",
            Left = 50,
            Top = 100
        };

        var text = new TextBox
        {
            Text = "Padded Centered Textbox",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 200,
            Height = 50
        };

        var label2 = new Label
        {
            Text = "Right Bottom Text",
            Left = -30,
            Top = -20,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var textButton2 = Button.CreateTextButton("Fixed Size Button");
        textButton2.Width = 110;
        textButton2.Height = 80;

        var panel1 = new Panel();
        panel1.Widgets.Add(label1);
        panel1.Widgets.Add(text);
        panel1.Widgets.Add(label2);
        panel1.Widgets.Add(textButton2);
        
        

			
        Title = "Hello World";
        Left = 547;
        Top = 269;
        MinWidth = 500;
        MinHeight = 200;
        ZIndex = -2;
        Content = panel1;

        CloseButton.Enabled = false;
        CloseButton.Visible = false;
    }
}