using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	internal class NodeManager {
		internal NodeState curNodeState;
		internal NodeState curState;
		internal ManeuverNode node = null;
		internal ManeuverNode nextNode = null;
		internal bool changed;
		internal bool encounter;
		internal bool resizeMainWindow;
		internal bool resizeClockWindow;

		internal bool progradeParsed = true;
		internal bool radialParsed = true;
		internal bool normalParsed = true;
		internal bool timeParsed = true;
		internal string progradeText = "";
		internal string radialText = "";
		internal string normalText = "";
		internal string timeText = "";

		internal NodeManager() {
			curState = new NodeState();
		}

		internal NodeManager(ManeuverNode n) {
			curState = new NodeState(n);
			curNodeState = new NodeState();
			node = n;
			updateCurrentNodeState();

			if (n.findNextEncounter() != null) {
				encounter = true;
			}
		}

		internal NodeManager nextState() {
			if (nextNode != null) {
				return new NodeManager(nextNode);
			}
			if (node.findNextEncounter() != null) {
				encounter = true;
			}
			return this;
		}

		internal void addPrograde(double d) {
			curState.deltaV.z += d;
			progradeText = curState.deltaV.z.ToString();
			changed = true;
		}

		internal void setPrograde(String s) {
			double d;
			progradeText = s;
			if (s.EndsWith(".")) {
				progradeParsed = false;
				return;
			}
			progradeParsed = double.TryParse(progradeText, out d);
			if (progradeParsed) {
				if (d != curState.deltaV.z) {
					progradeText = d.ToString();
					curState.deltaV.z = d;
					changed = true;
				}
			}
		}

		internal void addNormal(double d) {
			curState.deltaV.y += d;
			normalText = curState.deltaV.y.ToString();
			changed = true;
		}

		internal void setNormal(String s) {
			if (normalText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			normalText = s;
			if (s.EndsWith(".")) {
				normalParsed = false;
				return;
			}
			normalParsed = double.TryParse(normalText, out d);
			if (normalParsed) {
				if (d != curState.deltaV.y) {
					normalText = d.ToString();
					curState.deltaV.y = d;
					changed = true;
				}
			}
		}

		internal void addRadial(double d) {
			curState.deltaV.x += d;
			radialText = curState.deltaV.x.ToString();
			changed = true;
		}

		internal void setRadial(String s) {
			if (radialText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			radialText = s;
			if (s.EndsWith(".")) {
				radialParsed = false;
				return;
			}
			radialParsed = double.TryParse(radialText, out d);
			if (radialParsed) {
				if (d != curState.deltaV.x) {
					radialText = d.ToString();
					curState.deltaV.x = d;
					changed = true;
				}
			}
		}

		internal double currentUT() {
			return curState.UT;
		}

		internal void addUT(double d) {
			curState.UT += d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		internal void setUT(double d) {
			curState.UT = d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		internal void setUT(String s) {
			if (timeText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			timeText = s;
			if (s.EndsWith(".")) {
				timeParsed = false;
				return;
			}
			timeParsed = double.TryParse(timeText, out d);
			if (timeParsed) {
				if (d != curState.UT) {
					timeText = d.ToString();
					curState.UT = d;
					changed = true;
				}
			}
		}

		internal double currentMagnitude() {
			return curState.deltaV.magnitude;
		}

		internal void setPeriapsis() {
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToPe);
		}

		internal void setApoapsis() {
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToAp);
		}

		internal bool hasNode() {
			if (node == null) {
				return false;
			}
			return true;
		}

		internal void updateNode() {
			// Node manager policy:
			// if the manager has been changed from the last update manager snapshot, take the manager
			// UNLESS
			// if the node has been changed from the last update node snapshot, take the node
			if (curNodeState.compare(node)) {
				// the node hasn't changed, do our own thing
				if (changed) {
					if (node.attachedGizmo != null) {
						node.attachedGizmo.DeltaV = curState.getVector();
						node.attachedGizmo.UT = curState.UT;
					}
					node.OnGizmoUpdated(curState.getVector(), curState.UT);
					updateCurrentNodeState();
					changed = false; // new
				}
			} else {
				// the node has changed, take the node's new information for ourselves.
				updateCurrentNodeState();
				curState.update(node);
			}
		}

		private void updateCurrentNodeState() {
			curNodeState.update(node);
			progradeText = node.DeltaV.z.ToString();
			normalText = node.DeltaV.y.ToString();
			radialText = node.DeltaV.x.ToString();
			timeText = node.UT.ToString();
		}
	}
}
