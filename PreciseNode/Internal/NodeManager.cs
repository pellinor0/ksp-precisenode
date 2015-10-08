using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2014-2015, Maik Schreiber
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

	public static class OrbitExtensions
	{
		public static Vector3d DeltaVToManeuverNodeCoordinates(this Orbit o, double UT, Vector3d dV)
		{
			return new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
			                    Vector3d.Dot(o.NormalPlus(UT), dV),
			                    Vector3d.Dot(o.Prograde(UT), dV));
		}
		public static Vector3d Prograde(this Orbit o, double UT)
		{
			return o.SwappedOrbitalVelocityAtUT(UT)   .normalized;
		}
		public static Vector3d RadialPlus(this Orbit o, double UT)
		{
			return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
		}
		public static Vector3d Up(this Orbit o, double UT)
		{
			return o.SwappedRelativePositionAtUT(UT).normalized;
		}
		public static Vector3d NormalPlus(this Orbit o, double UT)
		{
			return SwapYZ(o.GetOrbitNormal()).normalized;
		}
		public static Vector3d SwappedOrbitalVelocityAtUT(this Orbit o, double UT)
		{
			return SwapYZ(o.getOrbitalVelocityAtUT(UT));
		}
		public static Vector3d SwappedRelativePositionAtUT(this Orbit o, double UT)
		{
			return SwapYZ(o.getRelativePositionAtUT(UT));
		}
		public static Vector3d SwapYZ(Vector3d v)
		{
			return new Vector3d(v.x, v.z, v.y);
		}
	}

	internal class NodeManager {
		internal ManeuverNode node;
		internal ManeuverNode nextNode;
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

		internal bool HasMemorized {
			get {
				return memory != null;
			}
		}

		private NodeState curNodeState;
		private NodeState curState;
		private bool changed;
		private NodeState memory;

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

		private void setPrograde(double d) {
			if (d != curState.deltaV.z) {
				curState.deltaV.z = d;
				changed = true;
			}
		}

		internal void addDv(Vector3d dV)
		{
			if (dV != Vector3d.zero) {
				curState.deltaV += dV;
				changed = true;
			}
		}

		internal void addPrograde(double d) {
			var ut = node.UT;
			// A prograde burn preserves the direction,
			// so there is no error to correct here.
			var dV = node.nextPatch.Prograde(ut) * d;
			dV = node.patch.DeltaVToManeuverNodeCoordinates (ut, dV);
			addDv (dV);
		}

		internal void setPrograde(String s) {
			if (!s.Equals(progradeText, StringComparison.Ordinal)) {
				progradeText = s;
				if (s.EndsWith(".")) {
					progradeParsed = false;
					return;
				}
				double d;
				progradeParsed = double.TryParse(progradeText, out d);
				if (progradeParsed) {
					setPrograde(d);
				}
			}
		}

		private void setNormal(double d) {
			if (d != curState.deltaV.y) {
				curState.deltaV.y = d;
				changed = true;
			}
		}

		internal void addNormal(double d) {
			var ut = node.UT;
			var vel = node.nextPatch.SwappedOrbitalVelocityAtUT (ut);
			// A normal burn turns the velocity vector around the
			//   axis through the vessel and the parent body.
			// Therefore it is only affected by the angular component of the velocity.
			var pos = node.nextPatch.SwappedRelativePositionAtUT (ut);
			var radVel = pos.normalized * Vector3d.Dot (pos.normalized, vel);
			var angVel = vel - radVel;
			// A full circle corresponds to a deltaV of 2*pi*angVel
			var angle = (d / angVel.magnitude) * (180d / Math.PI);
			var axis = -pos.normalized;
			var newVel = (Quaternion.AngleAxis((float)angle, axis) * vel);

			var dV = node.patch.DeltaVToManeuverNodeCoordinates (ut, newVel - vel);
			addDv (dV);
		}

		internal void setNormal(String s) {
			if (!s.Equals(normalText, StringComparison.Ordinal)) {
				normalText = s;
				if (s.EndsWith(".")) {
					normalParsed = false;
					return;
				}
				double d;
				normalParsed = double.TryParse(normalText, out d);
				if (normalParsed) {
					setNormal(d);
				}
			}
		}

		private void setRadial(double d) {
			if (d != curState.deltaV.x) {
				curState.deltaV.x = d;
				changed = true;
			}
		}

		internal void addRadial(double d) {
			var ut = node.UT;
			var vel = node.nextPatch.SwappedOrbitalVelocityAtUT (ut);
			// A radial burn turns the velocity vector around the orbit normal.
			// A full circle corresponds to a deltaV of 2*pi*vel
			var angle = (d / vel.magnitude) * (180d / Math.PI);
			var axis = node.nextPatch.NormalPlus(ut);
			var newVel = (Quaternion.AngleAxis((float)angle, axis) * vel);

			var dV = node.patch.DeltaVToManeuverNodeCoordinates (ut, newVel - vel);
			addDv (dV);
		}

		internal void setRadial(String s) {
			if (!s.Equals(radialText, StringComparison.Ordinal)) {
				radialText = s;
				if (s.EndsWith(".")) {
					radialParsed = false;
					return;
				}
				double d;
				radialParsed = double.TryParse(radialText, out d);
				if (radialParsed) {
					setRadial(d);
				}
			}
		}

		internal void memorize() {
			memory = (NodeState) curState.Clone();
		}

		internal void clearMemory() {
			memory = null;
		}

		internal void recallMemory() {
			setUT(memory.UT);
			setPrograde(memory.deltaV.z);
			setNormal(memory.deltaV.y);
			setRadial(memory.deltaV.x);
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
			progradeText = node.DeltaV.z.ToString("0.00");
			normalText = node.DeltaV.y.ToString("0.00");
			radialText = node.DeltaV.x.ToString("0.00");
			timeText = node.UT.ToString();
		}
	}
}
