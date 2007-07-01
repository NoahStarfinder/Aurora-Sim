/*
* Copyright (c) Contributors, http://www.openmetaverse.org/
* See CONTRIBUTORS.TXT for a full list of copyright holders.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of the OpenSim Project nor the
*       names of its contributors may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
* 
*/
using System;
using System.Collections.Generic;
using System.Text;
using Axiom.MathLib;
using OpenSim.Physics.Manager;
using libsecondlife;

namespace OpenSim.Region.Environment.Scenes
{
    public abstract class Entity :EntityBase //will be phased out 
    {     
        protected PhysicsActor _physActor;

        /// <summary>
        /// 
        /// </summary>
        public override LLVector3 Pos
        {
            get
            {
                if (this._physActor != null)
                {
                    m_pos.X = _physActor.Position.X;
                    m_pos.Y = _physActor.Position.Y;
                    m_pos.Z = _physActor.Position.Z;
                }

                return m_pos;
            }
            set
            {
                if (this._physActor != null)
                {
                    try
                    {
                        lock (this.m_world.SyncRoot)
                        {

                            this._physActor.Position = new PhysicsVector(value.X, value.Y, value.Z);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                m_pos = value;
            }
        }

       
        /// <summary>
        /// 
        /// </summary>
        public override LLVector3 Velocity
        {
            get
            {
                if (this._physActor != null)
                {
                    velocity.X = _physActor.Velocity.X;
                    velocity.Y = _physActor.Velocity.Y;
                    velocity.Z = _physActor.Velocity.Z;
                }

                return velocity;
            }
            set
            {
                if (this._physActor != null)
                {
                    try
                    {
                        lock (this.m_world.SyncRoot)
                        {

                            this._physActor.Velocity = new PhysicsVector(value.X, value.Y, value.Z);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                velocity = value;
            }
        }
    }
}
