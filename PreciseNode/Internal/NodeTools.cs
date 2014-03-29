using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

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

	internal static class NodeTools {
		/// <summary>
		/// Sets the conics render mode
		/// </summary>
		/// <param name="mode">The conics render mode to use, one of 0, 1, 2, 3, or 4.  Arguments outside those will be set to 3.</param>
		internal static void changeConicsMode(int mode) {
			switch(mode) {
				case 0:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_TO_BODIES;
					break;
				case 1:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_ENTRY_UT;
					break;
				case 2:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_EXIT_UT;
					break;
				case 3:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
					break;
				case 4:
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.DYNAMIC;
					break;
				default:
					// revert to KSP default
					FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
					break;
			}
		}

		/// <summary>
		/// Returns the orbit of the currently targeted item or null if there is none.
		/// </summary>
		/// <returns>The orbit or null.</returns>
		internal static Orbit getTargetOrbit() {
			ITargetable tgt = FlightGlobals.fetch.VesselTarget;
			if(tgt != null) {
				// if we have a null vessel it's a celestial body
				if(tgt.GetVessel() == null) { return tgt.GetOrbit(); }
				// otherwise make sure we're not targeting ourselves.
				if(!FlightGlobals.fetch.activeVessel.Equals(tgt.GetVessel())) {
					return tgt.GetOrbit();
				}
			}
			return null;
		}

		/// <summary>
		/// Convenience function.
		/// </summary>
		/// <returns>The patched conic solver for the currently active vessel.</returns>
		internal static PatchedConicSolver getSolver() {
			return FlightGlobals.ActiveVessel.patchedConicSolver;
		}

		/// <summary>
		/// Function to figure out which KeyCode was pressed.
		/// </summary>
		internal static KeyCode fetchKey() {
			int enums = System.Enum.GetNames(typeof(KeyCode)).Length;
			for(int k = 0; k < enums; k++) {
				if(Input.GetKey((KeyCode)k)) {
					return (KeyCode)k;
				}
			}

			return KeyCode.None;
		}
	}
}
