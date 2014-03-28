using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {
	public class PNOptions {
		public Rect mainWindowPos = new Rect(Screen.width / 10, 20, 250, 130);
		public Rect optionsWindowPos = new Rect(Screen.width / 3, 20, 250, 130);
		public Rect keymapperWindowPos = new Rect(Screen.width / 5, 20, 250, 130);
		public Rect clockWindowPos = new Rect(Screen.width / 3, Screen.height / 2, 195, 65);
		public Rect conicsWindowPos = new Rect(Screen.width / 5, Screen.height / 2, 250, 65);
		public Rect tripWindowPos = new Rect(Screen.width / 5, Screen.height / 5, 320, 65);

		public bool showManeuverPager = true;
		public bool showConicsAlways = false;
		public bool showClock = false;
		public bool showTrip = false;
		public bool showUTControls = false;
		public bool showEAngle = true;
		public bool showOrbitInfo = false;
		public bool removeUsedNodes = false;

		public bool largeUTIncrement = false;

		public KeyCode progInc = KeyCode.Keypad8;
		public KeyCode progDec = KeyCode.Keypad5;
		public KeyCode normInc = KeyCode.Keypad9;
		public KeyCode normDec = KeyCode.Keypad7;
		public KeyCode radiInc = KeyCode.Keypad6;
		public KeyCode radiDec = KeyCode.Keypad4;
		public KeyCode timeInc = KeyCode.Keypad3;
		public KeyCode timeDec = KeyCode.Keypad1;
		public KeyCode pageIncrement = KeyCode.Keypad0;
		public KeyCode pageConics = KeyCode.KeypadEnter;
		public KeyCode hideWindow = KeyCode.P;
		public KeyCode addWidget = KeyCode.O;
		public double increment = 1.0;
		public double usedNodeThreshold = 0.5;
		public int conicsMode = 3;

		public void downIncrement() {
			if (increment == 0.01) {
				increment = 0.1;
			} else if (increment == 0.1) {
				increment = 1;
			} else if (increment == 1) {
				increment = 10;
			} else if (increment == 10) {
				increment = 100;
			} else if (increment == 100) {
				increment = 0.01;
			} else {
				increment = 1;
			}
		}

		public void upIncrement() {
			if (increment == 0.01) {
				increment = 100;
			} else if (increment == 0.1) {
				increment = 0.01;
			} else if (increment == 1) {
				increment = 0.1;
			} else if (increment == 10) {
				increment = 1;
			} else if (increment == 100) {
				increment = 10;
			} else {
				increment = 1;
			}
		}

		public void setConicsMode(int mode) {
			conicsMode = mode;
			NodeTools.changeConicsMode(conicsMode);
		}

		public void pageConicsMode() {
			conicsMode++;
			if (conicsMode < 0 || conicsMode > 4) {
				conicsMode = 0;
			}
			NodeTools.changeConicsMode(conicsMode);
		}
	}
}
