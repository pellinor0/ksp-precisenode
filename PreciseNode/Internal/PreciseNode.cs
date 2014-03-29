using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2014, Maik Schreiber
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
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class PreciseNode : MonoBehaviour {
		private enum Key {
			NONE,
			PROGINC,
			PROGDEC,
			NORMINC,
			NORMDEC,
			RADIINC,
			RADIDEC,
			TIMEINC,
			TIMEDEC,
			PAGEINC,
			PAGECON,
			HIDEWINDOW,
			ADDWIDGET
		};
		
		internal PluginConfiguration config;

		private PNOptions options = new PNOptions();
		private NodeManager curState = new NodeManager();
		private List<Action> scheduledForLayout = new List<Action>();

		private bool configLoaded = false;
		private bool conicsLoaded = false;
		private bool shown = true;
		private bool showTimeNext = false;
		private bool waitForKey = false;
		private bool showOptions = false;
		private bool showKeymapper = false;
		private bool showEncounter = false;
		private Key currentWaitKey = Key.NONE;
		private double keyWaitTime = 0.0;

		private readonly int mainWindowId = new System.Random().Next(int.MaxValue);
		private readonly int optionsWindowId = new System.Random().Next(int.MaxValue);
		private readonly int keymapperWindowId = new System.Random().Next(int.MaxValue);
		private readonly int tripWindowId = new System.Random().Next(int.MaxValue);
		private readonly int clockWindowId = new System.Random().Next(int.MaxValue);
		private readonly int conicsWindowId = new System.Random().Next(int.MaxValue);

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		internal void Awake() {
			CancelInvoke();
			loadConfig();
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		internal void OnDisable() {
			saveConfig();
		}

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		internal void Update() {
			if(!FlightDriver.Pause && canShowNodeEditor) {
                PatchedConicSolver solver = NodeTools.getSolver();
                if(solver.maneuverNodes.Count > 0) {
                    if(!curState.hasNode() || !solver.maneuverNodes.Contains(curState.node)) {
                        // get the first one if we can't find the current or it's null
                        curState = new NodeManager(solver.maneuverNodes[0]);
                    } else if(curState.hasNode()) {
                        curState.updateNode();
                        curState = curState.nextState();
                    }
                } else {
                    if(curState.hasNode()) {
                        curState = new NodeManager();
                        curState.resizeClockWindow = true;
                    }
                }
                processKeyInput();
			}
		}

#if NODE_CLEANUP
        internal void FixedUpdate() {
            if(!FlightDriver.Pause) {
                PatchedConicSolver solver = NodeTools.getSolver();
                if(options.removeUsedNodes && solver.maneuverNodes.Count > 0) {
                    ManeuverNode node = solver.maneuverNodes[0];
                    if(node.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude < options.usedNodeThreshold) {
                        solver.RemoveManeuverNode(node);
                        //TODO: Clean up states after removing the node.
                    }
                }
            }
        }
#endif

		/// <summary>
		/// Overridden function from MonoBehavior
		/// </summary>
		internal void OnGUI() {
			// Porcess any scheduled functions
			if(Event.current.type == EventType.Layout && !FlightDriver.Pause && scheduledForLayout.Count > 0) {
				foreach(Action a in scheduledForLayout) {
					a();
				}
				scheduledForLayout.Clear();
			}
			if(canShowNodeEditor) {
				if(Event.current.type == EventType.Layout && !FlightDriver.Pause) {
					// On layout we should see if we have nodes to act on.
					if(curState.resizeMainWindow) {
						options.mainWindowPos.height = 0;
					}
					if(curState.resizeClockWindow) {
						options.clockWindowPos.height = 0;
					}
					showEncounter = curState.encounter;
					// this prevents the clock window from showing the time to
					// next node when the next state is created during repaint.
					showTimeNext = curState.hasNode();
				}
				if(!conicsLoaded) {
					NodeTools.changeConicsMode(options.conicsMode);
					conicsLoaded = true;
				}
				if(shown) {
					drawGUI();
				} else if(canShowConicsWindow) {
					drawConicsGUI();
				}
			} else if(canShowConicsWindow) {
				drawConicsGUI();
			}
			if(canShowClock) {
				drawClockGUI();
			}
		}

		/// <summary>
		/// Draw Node Editor and Options GUI
		/// </summary>
		private void drawGUI() {
			GUI.skin = null;
			options.mainWindowPos = GUILayout.Window(mainWindowId, options.mainWindowPos, (id) => drawMainWindow(),
				"Precise Node", GUILayout.ExpandHeight(true));
			if(showOptions) {
				options.optionsWindowPos = GUILayout.Window(optionsWindowId, options.optionsWindowPos, (id) => drawOptionsWindow(),
					"Precise Node Options", GUILayout.ExpandHeight(true));
			}
			if(showKeymapper) {
				options.keymapperWindowPos = GUILayout.Window(keymapperWindowId, options.keymapperWindowPos, (id) => drawKeymapperWindow(),
					"Precise Node Keys", GUILayout.ExpandHeight(true));
			}
			if(options.showTrip) {
				options.tripWindowPos = GUILayout.Window(tripWindowId, options.tripWindowPos, (id) => drawTripWindow(),
					"Trip Info", GUILayout.ExpandHeight(true));
			}
		}

		/// <summary>
		/// Draw Clock GUI
		/// </summary>
		private void drawClockGUI() {
			GUI.skin = null;
			options.clockWindowPos = GUILayout.Window(clockWindowId, options.clockWindowPos, (id) => drawClockWindow(),
				"Clock", GUILayout.ExpandHeight(true));
		}

		/// <summary>
		/// Draw Conics GUI
		/// </summary>
		private void drawConicsGUI() {
			GUI.skin = null;
			options.conicsWindowPos = GUILayout.Window(conicsWindowId, options.conicsWindowPos, (id) => drawConicsWindow(),
				"Conics Controls", GUILayout.ExpandHeight(true));
		}

		/// <summary>
		/// Draws the Node Editor window.
		/// </summary>
		private void drawMainWindow() {
			Color defaultColor = GUI.backgroundColor;
			Color contentColor = GUI.contentColor;
			Color curColor = defaultColor;
			PatchedConicSolver solver = NodeTools.getSolver();

			// Options button
			if(showOptions) { GUI.backgroundColor = Color.green; }
			if(GUI.Button(new Rect(options.mainWindowPos.width - 48, 2, 22, 18), "O")) {
				showOptions = !showOptions;
			}
			GUI.backgroundColor = defaultColor;

			// Keymapping button
			if(showKeymapper) { GUI.backgroundColor = Color.green; }
			if(GUI.Button(new Rect(options.mainWindowPos.width - 24, 2, 22, 18), "K")) {
				showKeymapper = !showKeymapper;
			}
			GUI.backgroundColor = defaultColor;

			GUILayout.BeginVertical();
			if(options.showManeuverPager) {
				GUIParts.drawManeuverPager(curState);
			}

			// Human-readable time
			GUIParts.drawDoubleLabel("Time:", 100, NodeTools.convertUTtoHumanTime(curState.currentUT()), 130);

			// Increment buttons
			GUILayout.BeginHorizontal();
			GUILayout.Label("Increment:", GUILayout.Width(100));
			GUIParts.drawButton("0.01", (options.increment == 0.01?Color.yellow:defaultColor), () => { options.increment = 0.01; });
			GUIParts.drawButton("0.1", (options.increment == 0.1?Color.yellow:defaultColor), () => { options.increment = 0.1; });
			GUIParts.drawButton("1", (options.increment == 1?Color.yellow:defaultColor), () => { options.increment = 1; });
			GUIParts.drawButton("10", (options.increment == 10?Color.yellow:defaultColor), () => { options.increment = 10; });
			GUIParts.drawButton("100", (options.increment == 100?Color.yellow:defaultColor), () => { options.increment = 100; });
			GUILayout.EndHorizontal();

			drawTimeControls(contentColor);
			drawProgradeControls(contentColor);
			drawNormalControls(contentColor);
			drawRadialControls(contentColor);

			// total delta-V display
			GUIParts.drawDoubleLabel("Total Δv:", 100, curState.currentMagnitude().ToString("0.##") + " m/s", 130);

			drawEAngle();
			drawEncounter(defaultColor);

			// Conics mode controls
			if (options.showConics) {
				GUIParts.drawConicsControls(options);
			}
			
			// trip info button and vessel focus buttons
			GUILayout.BeginHorizontal();
			GUIParts.drawButton("Trip Info", (options.showTrip?Color.yellow:defaultColor), () => { options.showTrip = !options.showTrip; });
			GUIParts.drawButton("Focus on Vessel", defaultColor, () => { MapView.MapCamera.SetTarget(FlightGlobals.ActiveVessel.vesselName); });
			GUILayout.EndHorizontal();
			
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		private void drawEAngle() {
			// Ejection angle
			if(options.showEAngle) {
				String eangle = "n/a";
				if(FlightGlobals.ActiveVessel.orbit.referenceBody.name != "Sun") {
					eangle = NodeTools.getEjectionAngle(FlightGlobals.ActiveVessel.orbit, curState.currentUT()).ToString("0.##") + "°";
				}
				GUIParts.drawDoubleLabel("Ejection angle:", 100, eangle, 130);
			}
		}

		private void drawEncounter(Color defaultColor) {
			// Additional Information
			if(options.showOrbitInfo) {
				// Find the next encounter, if any, in our flight plan.
				if(showEncounter) {
					Orbit nextEnc = NodeTools.findNextEncounter(curState.node);
					string name = "n/a";
					string theName = "n/a";
					string PeA = "n/a";

					if(nextEnc != null) {
						name = nextEnc.referenceBody.name;
						theName = nextEnc.referenceBody.theName;
						PeA = NodeTools.formatMeters(nextEnc.PeA);
					} else {
						curState.encounter = false;
					}
					// Next encounter periapsis
					GUIParts.drawDoubleLabel("(" + name + ") Pe:", 100, PeA, 130);
					GUILayout.BeginHorizontal();
					GUILayout.Label("", GUILayout.Width(100));
					GUIParts.drawButton("Focus on " + theName, defaultColor, () => { MapView.MapCamera.SetTarget(name); });
					GUILayout.EndHorizontal();
				} else {
					if(curState.node.solver.flightPlan.Count > 1) {
						// output the apoapsis and periapsis of our projected orbit.
						GUIParts.drawDoubleLabel("Apoapsis:", 100, NodeTools.formatMeters(curState.node.nextPatch.ApA), 100);
						GUIParts.drawDoubleLabel("Periapsis:", 100, NodeTools.formatMeters(curState.node.nextPatch.PeA), 130);
					}
				}
			}
		}

		private void drawTimeControls(Color contentColor) {
			Color defaultColor = GUI.backgroundColor;

			// Universal time controls
			GUILayout.BeginHorizontal();
			GUILayout.Label((options.largeUTIncrement?"UT: (x10 inc)":"UT:"), GUILayout.Width(100));
			GUI.backgroundColor = Color.green;
			if(!curState.timeParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.timeText, GUILayout.Width(100));
			if(!curState.timeText.Equals(check, StringComparison.Ordinal)) {
				curState.setUT(check);
			}
			GUI.contentColor = contentColor;
			double ut_increment = options.increment * (options.largeUTIncrement ? 10.0 : 1.0);
			GUIParts.drawButton("-", Color.red, () => { curState.addUT(ut_increment * -1.0); });
			GUIParts.drawButton("+", Color.green, () => { curState.addUT(ut_increment); });
			GUILayout.EndHorizontal();

			// extended time controls
			if(options.showUTControls) {
				GUILayout.BeginHorizontal();
				GUIParts.drawButton("Peri", Color.yellow, () => { curState.setPeriapsis(); });
				GUIParts.drawButton("DN", Color.magenta, delegate() {
					Orbit targ = NodeTools.getTargetOrbit();
					if(targ != null) {
						curState.setUT(NodeTools.getTargetDNUT(curState.node.patch, targ));
					} else {
						curState.setUT(NodeTools.getEquatorialDNUT(curState.node.patch));
					}
				});
				if (options.largeUTIncrement) {
					GUIParts.drawButton("-Orb", Color.red, () => { curState.addUT(-curState.node.patch.period); });
					GUIParts.drawButton("+Orb", Color.green, () => { curState.addUT(curState.node.patch.period); });
				} else {
					GUIParts.drawButton("-1K", Color.red, () => { curState.addUT(-1000); });
					GUIParts.drawButton("+1K", Color.green, () => { curState.addUT(1000); });
				}
				GUIParts.drawButton("AN", Color.cyan, delegate() {
					Orbit targ = NodeTools.getTargetOrbit();
					if(targ != null) {
						curState.setUT(NodeTools.getTargetANUT(curState.node.patch, targ));
					} else {
						curState.setUT(NodeTools.getEquatorialANUT(curState.node.patch));
					}
				});
				GUIParts.drawButton("Apo", Color.blue, () => { curState.setApoapsis(); });
				GUILayout.EndHorizontal();
			}

			GUI.backgroundColor = defaultColor;
		}

		private void drawProgradeControls(Color contentColor) {
			// Prograde controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Prograde:", GUILayout.Width(100));
			if(!curState.progradeParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.progradeText, GUILayout.Width(100));
			if(!curState.progradeText.Equals(check, StringComparison.Ordinal)) {
				curState.setPrograde(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, () => { curState.addPrograde(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, () => { curState.addPrograde(options.increment); });
			GUILayout.EndHorizontal();
		}

		private void drawNormalControls(Color contentColor) {
			// Normal controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Normal:", GUILayout.Width(100));
			if(!curState.normalParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.normalText, GUILayout.Width(100));
			if(!curState.normalText.Equals(check, StringComparison.Ordinal)) {
				curState.setNormal(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, () => { curState.addNormal(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, () => { curState.addNormal(options.increment); });
			GUILayout.EndHorizontal();
		}

		private void drawRadialControls(Color contentColor) {
			// radial controls
			GUILayout.BeginHorizontal();
			GUILayout.Label("Radial:", GUILayout.Width(100));
			if(!curState.radialParsed) {
				GUI.contentColor = Color.red;
			}
			string check = GUILayout.TextField(curState.radialText, GUILayout.Width(100));
			if(!curState.radialText.Equals(check, StringComparison.Ordinal)) {
				curState.setRadial(check);
			}
			GUI.contentColor = contentColor;
			GUIParts.drawButton("-", Color.red, () => { curState.addRadial(options.increment * -1.0); });
			GUIParts.drawButton("+", Color.green, () => { curState.addRadial(options.increment); });
			GUILayout.EndHorizontal();
		}

		/// <summary>
		/// Draws the Clock window.
		/// </summary>
		private void drawClockWindow() {
			Color defaultColor = GUI.backgroundColor;
			double timeNow = Planetarium.GetUniversalTime();
			String timeUT = timeNow.ToString("F0");
			String timeHuman = NodeTools.convertUTtoHumanTime(timeNow);

			GUILayout.BeginVertical();

			GUIParts.drawDoubleLabel("Time:", 35, timeHuman, 150);
			GUIParts.drawDoubleLabel("UT:", 35, Math.Floor(timeNow).ToString("F0"), 150);

			if(showTimeNext) {
				double next = 0.0;
				string labelText = "";
				if(NodeTools.getSolver().maneuverNodes.Count > 0) {
					// protection from index out of range errors.
					// should probably handle this better.
					next = timeNow - NodeTools.getSolver().maneuverNodes[0].UT;
				}
				if(next < 0) {
					labelText = "T- " + NodeTools.convertUTtoHumanDuration(next);
				} else {
					labelText = "T+ " + NodeTools.convertUTtoHumanDuration(next);
				}
				GUIParts.drawDoubleLabel("Next:", 35, labelText, 150);
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Conics window.
		/// </summary>
		private void drawConicsWindow() {
			GUILayout.BeginVertical();
			GUIParts.drawConicsControls(options);
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Options window.
		/// </summary>
		private void drawOptionsWindow() {
			Color defaultColor = GUI.backgroundColor;

			// Close button
			if(GUI.Button(new Rect(options.optionsWindowPos.width - 24, 2, 22, 18), "X")) {
				showOptions = false;
			}

			GUILayout.BeginVertical();

			// use a temp variable so we can check whether the main window needs resizing.
			bool temp;

			temp = GUILayout.Toggle(options.showConics, "Show conics controls");
			if (temp != options.showConics) {
				options.showConics = temp;
				curState.resizeMainWindow = true;
			}

			options.showConicsAlways = GUILayout.Toggle(options.showConicsAlways, "Show conics window");
			options.showClock = GUILayout.Toggle(options.showClock, "Show clock");
			temp = GUILayout.Toggle(options.showManeuverPager, "Show maneuver pager");
			if(temp != options.showManeuverPager) {
				options.showManeuverPager = temp;
				curState.resizeMainWindow = true;
			}
			temp = GUILayout.Toggle(options.showUTControls, "Show additional UT controls");
			if(temp != options.showUTControls) {
				options.showUTControls = temp;
				curState.resizeMainWindow = true;
			}
			options.largeUTIncrement = GUILayout.Toggle(options.largeUTIncrement, "Use x10 UT increment");
			temp = GUILayout.Toggle(options.showEAngle, "Show ejection angle");
			if(temp != options.showEAngle) {
				options.showEAngle = temp;
				curState.resizeMainWindow = true;
			}
			temp = GUILayout.Toggle(options.showOrbitInfo, "Show orbit information");
			if(temp != options.showOrbitInfo) {
				options.showOrbitInfo = temp;
				curState.resizeMainWindow = true;
			}
#if NODE_CLEANUP
			options.removeUsedNodes = GUILayout.Toggle(options.removeUsedNodes, "Remove used nodes");
            //TODO: Add threshold controls for removing used nodes
#endif
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Draws the Keymapper window.
		/// </summary>
		private void drawKeymapperWindow() {
			Color defaultColor = GUI.backgroundColor;

			// Close button
			if(GUI.Button(new Rect(options.keymapperWindowPos.width - 24, 2, 22, 18), "X")) {
				showKeymapper = false;
			}

			GUILayout.BeginVertical();

			// Set window control
			drawKeyControls("Hide/show window", Key.HIDEWINDOW, options.hideWindow);

			// Set add node widget button
			drawKeyControls("Open node gizmo", Key.ADDWIDGET, options.addWidget);

			// Set prograde controls
			drawKeyControls("Increment prograde", Key.PROGINC, options.progInc);
			drawKeyControls("Decrement prograde", Key.PROGDEC, options.progDec);

			// set normal controls
			drawKeyControls("Increment normal", Key.NORMINC, options.normInc);
			drawKeyControls("Decrement normal", Key.NORMDEC, options.normDec);

			// set radial controls
			drawKeyControls("Increment radial", Key.RADIINC, options.radiInc);
			drawKeyControls("Decrement radial", Key.RADIDEC, options.radiDec);

			// set time controls
			drawKeyControls("Increment time", Key.TIMEINC, options.timeInc);
			drawKeyControls("Decrement time", Key.TIMEDEC, options.timeDec);

			// set paging controls
			drawKeyControls("Page increment", Key.PAGEINC, options.pageIncrement);
			drawKeyControls("Page conics", Key.PAGECON, options.pageConics);

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		private void drawKeyControls(string title, Key key, KeyCode currentKeyCode) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(title + ": " + currentKeyCode.ToString(), GUILayout.Width(200));
			GUIParts.drawButton("Set", GUI.backgroundColor, () => {
				doWaitForKey("Press a key to bind " + title.ToLower() + "...", key);
			});
			GUILayout.EndHorizontal();
		}

		private void drawTripWindow() {
			PatchedConicSolver solver = NodeTools.getSolver();

			GUILayout.BeginVertical();
			if(solver.maneuverNodes.Count < 1) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("No nodes to show.", GUILayout.Width(200));
				GUILayout.EndHorizontal();
			} else {
				double total = 0.0;
				double timeNow = Planetarium.GetUniversalTime();

				GUILayout.BeginHorizontal();
				GUILayout.Label("", GUILayout.Width(60));
				GUILayout.Label("Δv", GUILayout.Width(90));
				GUILayout.Label("Time Until", GUILayout.Width(200));
				GUILayout.Label("", GUILayout.Width(120));
				GUILayout.EndHorizontal();

				foreach(ManeuverNode curNode in solver.maneuverNodes) {
					int idx = solver.maneuverNodes.IndexOf(curNode);
					double timeDiff = curNode.UT - timeNow;
					GUILayout.BeginHorizontal();
					GUILayout.Label("Node " + (idx + 1), GUILayout.Width(60));
					GUILayout.Label(curNode.DeltaV.magnitude.ToString("F2") + " m/s", GUILayout.Width(90));
					GUILayout.Label(NodeTools.convertUTtoHumanDuration(timeDiff), GUILayout.Width(200));
					// these will be scheduled for during the next layout pass
					if(idx > 0) {
						GUIParts.drawButton("Merge ▲", Color.white, delegate() {scheduledForLayout.Add(new Action(() => {NodeTools.mergeNodeDown(solver.maneuverNodes[idx]);}));});
					}
					GUILayout.EndHorizontal();
					total += curNode.DeltaV.magnitude;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("Total", GUILayout.Width(60));
				GUILayout.Label(total.ToString("F2") + " m/s", GUILayout.Width(90));
				GUILayout.Label("", GUILayout.Width(200));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		/// <summary>
		/// Returns whether the Node Editor can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Node Editor can be shown; otherwise, <c>false</c>.</value>
		private bool canShowNodeEditor {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && NodeTools.getSolver().maneuverNodes.Count > 0;
			}
		}

		/// <summary>
		/// Returns whether the Conics Window can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Conics Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowConicsWindow {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled && options.showConicsAlways;
			}
		}

		/// <summary>
		/// Returns whether the Clock Window can be shown based on a number of global factors.
		/// </summary>
		/// <value><c>true</c> if the Clock Window can be shown; otherwise, <c>false</c>.</value>
		private bool canShowClock {
			get {
				return FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && RenderingManager.fetch.enabled && options.showClock;
			}
		}


		private void doWaitForKey(String msg, Key key) {
			ScreenMessages.PostScreenMessage(msg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
			waitForKey = true;
			currentWaitKey = key;
			keyWaitTime = Planetarium.GetUniversalTime();
		}

		/// <summary>
		/// Processes keyboard input.
		/// </summary>
		private void processKeyInput() {
			if(!Input.anyKeyDown) {
				return;
			}

			// Fix for a bug in Linux where typing would still control game elements even if
			// a textbox was focused.
			if(GUIUtility.keyboardControl != 0) {
				return;
			}

			// process any key input for settings
			if(waitForKey) {
				KeyCode key = NodeTools.fetchKey();
				// if the time is up or we have no key to process, reset.
				if(((keyWaitTime + 5.0) < Planetarium.GetUniversalTime()) || key == KeyCode.None) {
					currentWaitKey = Key.NONE;
					waitForKey = false;
					return;
				}

				// which key are we waiting for?
				switch(currentWaitKey) {
					case Key.PROGINC:
						options.progInc = key;
						break;
					case Key.PROGDEC:
						options.progDec = key;
						break;
					case Key.NORMINC:
						options.normInc = key;
						break;
					case Key.NORMDEC:
						options.normDec = key;
						break;
					case Key.RADIINC:
						options.radiInc = key;
						break;
					case Key.RADIDEC:
						options.radiDec = key;
						break;
					case Key.TIMEINC:
						options.timeInc = key;
						break;
					case Key.TIMEDEC:
						options.timeDec = key;
						break;
					case Key.PAGEINC:
						options.pageIncrement = key;
						break;
					case Key.PAGECON:
						options.pageConics = key;
						break;
					case Key.HIDEWINDOW:
						options.hideWindow = key;
						break;
					case Key.ADDWIDGET:
						options.addWidget = key;
						break;
				}
				currentWaitKey = Key.NONE;
				waitForKey = false;
				return;
			}

			// process normal keyboard input
			// change increment
			if(Input.GetKeyDown(options.pageIncrement)) {
				if(Event.current.alt) {
					options.downIncrement();
				} else {
					options.upIncrement();
				}
			}
			// prograde increment
			if(Input.GetKeyDown(options.progInc)) {
				curState.addPrograde(options.increment);
			}
			// prograde decrement
			if(Input.GetKeyDown(options.progDec)) {
				curState.addPrograde(options.increment * -1.0);
			}
			// normal increment
			if(Input.GetKeyDown(options.normInc)) {
				curState.addNormal(options.increment);
			}
			// normal decrement
			if(Input.GetKeyDown(options.normDec)) {
				curState.addNormal(options.increment * -1.0);
			}
			// radial increment
			if(Input.GetKeyDown(options.radiInc)) {
				curState.addRadial(options.increment);
			}
			// radial decrement
			if(Input.GetKeyDown(options.radiDec)) {
				curState.addRadial(options.increment * -1.0);
			}
			// UT increment
			if(Input.GetKeyDown(options.timeInc)) {
				curState.addUT(options.increment * (options.largeUTIncrement ? 10.0 : 1.0));
			}
			// UT decrement
			if(Input.GetKeyDown(options.timeDec)) {
				curState.addUT(options.increment * (options.largeUTIncrement ? -10.0 : -1.0));
			}
			// Page Conics
			if(Input.GetKeyDown(options.pageConics)) {
				options.pageConicsMode();
			}
			// hide/show window
			if(Input.GetKeyDown(options.hideWindow)) {
				shown = !shown;
			}
			// open node gizmo
			if(Input.GetKeyDown(options.addWidget)) {
				NodeTools.CreateNodeGizmo(curState.node);
			}
		}

		/// <summary>
		/// Load any saved configuration from file.
		/// </summary>
		private void loadConfig() {
			Debug.Log("Loading PreciseNode settings.");
			if(!configLoaded) {
				config = KSP.IO.PluginConfiguration.CreateForType<PreciseNode>(null);
				config.load();
				configLoaded = true;

				try {
					options.conicsMode = config.GetValue<int>("conicsMode", 3);
					options.mainWindowPos.x = config.GetValue<int>("mainWindowX", Screen.width / 10);
					options.mainWindowPos.y = config.GetValue<int>("mainWindowY", 20);
					options.optionsWindowPos.x = config.GetValue<int>("optWindowX", Screen.width / 3);
					options.optionsWindowPos.y = config.GetValue<int>("optWindowY", 20);
					options.keymapperWindowPos.x = config.GetValue<int>("keyWindowX", Screen.width / 5);
					options.keymapperWindowPos.y = config.GetValue<int>("keyWindowY", 20);
					options.clockWindowPos.x = config.GetValue<int>("clockWindowX", Screen.width / 3);
					options.clockWindowPos.y = config.GetValue<int>("clockWindowY", Screen.height / 2);
					options.conicsWindowPos.x = config.GetValue<int>("conicsWindowX", Screen.width / 5);
					options.conicsWindowPos.y = config.GetValue<int>("conicsWindowY", Screen.height / 2);
					options.tripWindowPos.x = config.GetValue<int>("tripWindowX", Screen.width / 5);
					options.tripWindowPos.y = config.GetValue<int>("tripWindowY", Screen.height / 5);
					options.showClock = config.GetValue<bool>("showClock", false);
					options.showEAngle = config.GetValue<bool>("showEAngle", true);
					options.showConics = config.GetValue<bool>("showConics", true);
					options.showConicsAlways = config.GetValue<bool>("showConicsAlways", false);
					options.showOrbitInfo = config.GetValue<bool>("showOrbitInfo", false);
					options.showUTControls = config.GetValue<bool>("showUTControls", false);
					options.showManeuverPager = config.GetValue<bool>("showManeuverPager", true);
#if NODE_CLEANUP
					options.removeUsedNodes = config.GetValue<bool>("removeUsedNodes", false);
					options.usedNodeThreshold = config.GetValue<double>("usedNodeThreshold", 0.5);
#endif
					options.largeUTIncrement = config.GetValue<bool>("largeUTIncrement", false);

					string temp = config.GetValue<String>("progInc", "Keypad8");
					options.progInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("progDec", "Keypad5");
					options.progDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normInc", "Keypad9");
					options.normInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("normDec", "Keypad7");
					options.normDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiInc", "Keypad6");
					options.radiInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("radiDec", "Keypad4");
					options.radiDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeInc", "Keypad3");
					options.timeInc = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("timeDec", "Keypad1");
					options.timeDec = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageIncrement", "Keypad0");
					options.pageIncrement = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("pageConics", "KeypadEnter");
					options.pageConics = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("hideWindow", "P");
					options.hideWindow = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
					temp = config.GetValue<String>("addWidget", "O");
					options.addWidget = (KeyCode)Enum.Parse(typeof(KeyCode), temp);
				} catch(ArgumentException) {
					// do nothing here, the defaults are already set
				}
			}
		}

		/// <summary>
		/// Save our configuration to file.
		/// </summary>
		private void saveConfig() {
			Debug.Log("Saving PreciseNode settings.");
			if(!configLoaded) {
				config = KSP.IO.PluginConfiguration.CreateForType<PreciseNode>(null);
			}

			config["conicsMode"] = options.conicsMode;
			config["progInc"] = options.progInc.ToString();
			config["progDec"] = options.progDec.ToString();
			config["normInc"] = options.normInc.ToString();
			config["normDec"] = options.normDec.ToString();
			config["radiInc"] = options.radiInc.ToString();
			config["radiDec"] = options.radiDec.ToString();
			config["timeInc"] = options.timeInc.ToString();
			config["timeDec"] = options.timeDec.ToString();
			config["pageIncrement"] = options.pageIncrement.ToString();
			config["pageConics"] = options.pageConics.ToString();
			config["hideWindow"] = options.hideWindow.ToString();
			config["addWidget"] = options.addWidget.ToString();
			config["mainWindowX"] = (int)options.mainWindowPos.x;
			config["mainWindowY"] = (int)options.mainWindowPos.y;
			config["optWindowX"] = (int)options.optionsWindowPos.x;
			config["optWindowY"] = (int)options.optionsWindowPos.y;
			config["keyWindowX"] = (int)options.keymapperWindowPos.x;
			config["keyWindowY"] = (int)options.keymapperWindowPos.y;
			config["clockWindowX"] = (int)options.clockWindowPos.x;
			config["clockWindowY"] = (int)options.clockWindowPos.y;
			config["conicsWindowX"] = (int)options.conicsWindowPos.x;
			config["conicsWindowY"] = (int)options.conicsWindowPos.y;
			config["tripWindowX"] = (int)options.tripWindowPos.x;
			config["tripWindowY"] = (int)options.tripWindowPos.y;
			config["showClock"] = options.showClock;
			config["showEAngle"] = options.showEAngle;
			config["showConics"] = options.showConics;
			config["showConicsAlways"] = options.showConicsAlways;
			config["showOrbitInfo"] = options.showOrbitInfo;
			config["showUTControls"] = options.showUTControls;
			config["showManeuverPager"] = options.showManeuverPager;
#if NODE_CLEANUP
			config["removeUsedNodes"] = options.removeUsedNodes;
			config["usedNodeThreshold"] = options.usedNodeThreshold;
#endif
			config["largeUTIncrement"] = options.largeUTIncrement;

			config.save();
		}
	}	
}

