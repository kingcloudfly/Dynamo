﻿//Copyright 2013 Ian Keough

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System.Windows;
using Dynamo.Utilities;

namespace Dynamo.Nodes
{
    /// <summary>
    /// Interaction logic for dynScriptEditWindow.xaml
    /// </summary>
    public partial class dynScriptEditWindow : Window
    {
        public dynScriptEditWindow()
        {
            InitializeComponent();
            this.Owner = dynSettings.Bench;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //cance = return false
            this.DialogResult = false;
        }
    }
}
