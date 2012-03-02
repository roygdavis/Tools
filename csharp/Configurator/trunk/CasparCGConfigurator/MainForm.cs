﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace CasparCGConfigurator
{
    public partial class MainForm : Form
    {
        public configuration config = new configuration();
        private ConsumerControlBase consumerEditorControl;
        private AbstractConsumer lastConsumer;
        
        public MainForm()
        {
            this.InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (System.IO.File.Exists("casparcg.config"))
                DeSerializeConfig(System.IO.File.ReadAllText("casparcg.config"));
            else
                SerializeConfig();   
            this.WireBindings();
            this.Updatechannel();
        }
        
        private void WireBindings()
        {
            this.pathsBindingSource.DataSource = this.config.Paths;
            this.configurationBindingSource.DataSource = this.config;
            this.listBox1.DataSource = this.config.Channels;           
        }

        private void SerializeConfig()
        {
            var extraTypes = new Type[1]{typeof(AbstractConsumer)};

            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using(var writer = doc.CreateWriter())
            {
                new XmlSerializer(typeof(configuration), extraTypes).Serialize(writer, config, namespaces);
            }

            doc.Element("configuration").Add(
                new XElement("controllers",
                    new XElement("tcp",
                        new XElement[2]
                        {
                            new XElement("port", 5220),
                            new XElement("protocol", "AMCP")
                        })));

            using (var writer = new XmlTextWriter("casparcg.config", new UTF8Encoding(false, false))) // No BOM
            {
                doc.Save(writer);
            }
        }
        
        private void DeSerializeConfig(string text)
        {
            var x = new XmlSerializer(typeof(configuration));

            using (var reader = new StringReader(text))
            {
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);

                this.config = (configuration)x.Deserialize(reader);
            }
            this.WireBindings();
        }

        private void RefreshConsumerPanel()
        {
            if (lastConsumer != listBox2.SelectedItem)
            {

                this.panel1.Controls.Clear();
                if (consumerEditorControl != null)
                    consumerEditorControl.Dispose();

                this.consumerEditorControl = null;

                if (listBox2.SelectedItems.Count > 0)
                {
                    if (listBox2.SelectedItem.GetType() == typeof(DecklinkConsumer))
                    {
                        this.consumerEditorControl = new DecklinkConsumerControl(listBox2.SelectedItem as DecklinkConsumer);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(ScreenConsumer))
                    {
                        this.consumerEditorControl = new ScreenConsumerControl(listBox2.SelectedItem as ScreenConsumer);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                    else if (listBox2.SelectedItem.GetType() == typeof(BluefishConsumer))
                    {
                        this.consumerEditorControl = new BluefishConsumerControl(listBox2.SelectedItem as BluefishConsumer);
                        this.panel1.Controls.Add(consumerEditorControl);
                    }
                }
            }
            lastConsumer = (AbstractConsumer)listBox2.SelectedItem;
        }

        private void Updatechannel()
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                this.comboBox1.Enabled = true;
                this.listBox2.Enabled = true;
                this.button4.Enabled = true;
                this.button5.Enabled = true;
                this.button7.Enabled = true;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.listBox2.DataSource = ((Channel)listBox1.SelectedItem).Consumers;
                this.comboBox1.SelectedItem = ((Channel)listBox1.SelectedItem).VideoMode;
            }
            else
            {
                this.comboBox1.Enabled = false;
                this.listBox2.Enabled = false;
                this.button4.Enabled = false;
                this.button5.Enabled = false;
                this.button7.Enabled = false;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.listBox2.DataSource = null;
                this.comboBox1.SelectedItem = null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.config.Channels.AddNew();
            this.Updatechannel();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Updatechannel();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new DecklinkConsumer());

            RefreshConsumerPanel();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new ScreenConsumer());
            this.RefreshConsumerPanel();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)            
                (listBox1.SelectedItem as Channel).VideoMode = comboBox1.SelectedItem.ToString();            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)            
                this.config.Channels.Remove((Channel)listBox1.SelectedItem);
            
            this.Updatechannel();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)            
                (listBox1.SelectedItem as Channel).Consumers.Remove(listBox2.SelectedItem as AbstractConsumer);
            
            this.RefreshConsumerPanel();
        }

        private void showXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SerializeConfig();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.RefreshConsumerPanel();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var res = System.Windows.Forms.MessageBox.Show("Do you want to save this configuration before exiting?", "CasparCG Configurator", MessageBoxButtons.YesNoCancel);
            if (res == System.Windows.Forms.DialogResult.Yes || res == System.Windows.Forms.DialogResult.OK)
                SerializeConfig();
            //else if(res == System.Windows.Forms.DialogResult.No)           
            else if(res == System.Windows.Forms.DialogResult.Cancel)            
                e.Cancel = true; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new SystemAudioConsumer());
            RefreshConsumerPanel();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            (listBox2.DataSource as BindingList<AbstractConsumer>).Add(new BluefishConsumer());
            RefreshConsumerPanel();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            using (var fd = new FolderBrowserDialog())
            {
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    datapathTextBox.Text = fd.SelectedPath;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            using (var fd = new FolderBrowserDialog())
            {
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)                
                    logpathTextBox.Text = fd.SelectedPath;                
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            using (var fd = new FolderBrowserDialog())
            {
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)                
                    mediapathTextBox.Text = fd.SelectedPath;                
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            using(var fd = new FolderBrowserDialog())
            {
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)                
                    templatepathTextBox.Text = fd.SelectedPath;                
            }
        }
    }
}
