using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/******************************************************************************
 * Copyright (c) 2014, Justin Bengtson
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
    internal class ModuleNodeSaver : PartModule {
		[KSPField (isPersistant = true)]
		NodeList nodes;

		internal ModuleNodeSaver() {
			nodes = new NodeList(this);
		}

		public override void OnInitialize() {
			if(!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null || this.vessel == null) {
				return;
			} else if(this.vessel.patchedConicSolver == null) {
				return;
			}

			PatchedConicSolver p = this.vessel.patchedConicSolver;

			// don't load if we've already got nodes.
			if(p.maneuverNodes.Count > 0) { return; }

			foreach(NodeState n in nodes.nodes) {
				// make sure we have a UT here and that it's in the future
				if(n.UT > Planetarium.GetUniversalTime()) {
					n.createManeuverNode(p);
				}
			}
		}
    }
}
