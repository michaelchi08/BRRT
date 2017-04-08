﻿using System;
using System.Collections.Generic;
using System.Drawing;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet;

namespace BRRT
{
	public class RRT
	{
		/// <summary>
		/// The map we are working on.
		/// </summary>
		public Map InternalMap{get;private set;}
		/// <summary>
		/// Start Point of the RRT.
		/// </summary>
		/// <value>The start point.</value>
		public Point StartPoint{ get; private set; }
		/// <summary>
		/// The Robot orientation at the start point.
		/// </summary>
		/// <value>The start orientation.</value>
		public double StartOrientation{get;private set;}
		/// <summary>
		/// The start RRT node.
		/// </summary>
		public RRTNode StartRRTNode{get;private set;}
		/// <summary>
		/// EndPoint of the RRT
		/// </summary>
		/// <value>The end point.</value>
		public Point EndPoint{get;private set;}
		/// <summary>
		/// The orientation of the robot at the endpoint
		/// </summary>
		/// <value>The end orientation.</value>
		public double EndOrientation{get;private set;}
		/// <summary>
		/// The EndPoint and Orientation summarized in an RRTNode
		/// </summary>
		public RRTNode EndRRTNode{get;private set;}
		/// <summary>
		/// Gets or sets the amount of iterations.
		/// </summary>
		/// <value>The iterations.</value>
		public UInt32 Iterations { get; set; }
		/// <summary>
		/// Occurs when algorithm has finished.
		/// </summary>
		public event EventHandler<EventArgs> Finished;

		/// <summary>
		/// List of All valid nodes so we don't need to iterate over the tree spanned from the StartPoint.
		/// </summary>
		private List<RRTNode> AllNodes = new List<RRTNode>();

		/// <summary>
		/// Gets or sets the minumum radius.
		/// </summary>
		/// <value>The minumum radius.</value>
		public double MinumumRadius { get; set; }

		/// <summary>
		/// Gets or sets the maximum drift.
		/// </summary>
		/// <value>The maximum drift.</value>
		public double MaximumDrift { get; set; }

		/// <summary>
		/// The width of one incremental step when stepping towards new node
		/// </summary>
		/// <value>The width of the step.</value>
		public int StepWidth { get; set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="BRRT.RRT"/> class.
		/// </summary>
		/// <param name="_Map">Map.</param>
		public RRT (Map _Map)
		{
			this.InternalMap = _Map;
			this.Iterations = 1002;
			this.MaximumDrift = 20;
			this.StepWidth = 10;
		}

		/// <summary>
		/// Start the RRT.
		/// </summary>
		/// <param name="_Start">Start.</param>
		/// <param name="_StartOrientation">Start orientation.</param>
		/// <param name="_End">End.</param>
		/// <param name="_EndOrientation">End orientation.</param>
		public void Start(Point _Start, double _StartOrientation, Point _End , double _EndOrientation)
		{
			this.StartPoint = InternalMap.FromMapCoordinates(_Start);
			this.StartOrientation = _StartOrientation;
			this.StartRRTNode = new RRTNode (StartPoint, StartOrientation, null);
			this.EndPoint = InternalMap.FromMapCoordinates(_End);
			this.EndOrientation = _EndOrientation;
			this.EndRRTNode = new RRTNode (EndPoint, EndOrientation, null);

			this.AllNodes.Add (StartRRTNode);
			//Do n iterations of the algorithm
			for (UInt32 it = 0; it < Iterations; it++) {
				DoStep ();
			}
			if (Finished != null)
				Finished (this, new EventArgs ());
		}
		/// <summary>
		/// Do a step into the right direction
		/// </summary>
		private void DoStep()
		{
			//First go straight
			//Select a random base node from the list of all nodes
			RRTNode RandomNode = RRTHelpers.SelectRandomNode(AllNodes);

			//Get a new straight or drift random node
			RRTNode NewStraightNode = RRTHelpers.GetRandomStraightPoint (RandomNode, this.MaximumDrift);
			//Now step to the new node
			StepToNode (RandomNode, NewStraightNode, true);
		}
		/// <summary>
		/// Steps to node.
		/// Takes a start and an end node and the argument wether to go straight or in a circle.
		/// Steps into this direction
		/// </summary>
		/// <param name="Start">Start.</param>
		/// <param name="End">End.</param>
		/// <param name="straight">If set to <c>true</c> straight.</param>
		private void StepToNode(RRTNode Start, RRTNode End, bool straight)
		{
			if (straight) {
				//Linear equation between points: y = mx +b
				double m = ((double)Start.Position.Y - (double)End.Position.Y)/((double)Start.Position.X- (double)End.Position.X);
				double b = (double)Start.Position.Y - m * (double)Start.Position.X;
				RRTNode lastFoundNode = null;

				//Lambda function that calculates a new point from a given x value
				//Checks if the node is valid 
				//Adds it into the list of nodes.
				//Returns false if point not valid 
				Func<int,bool> CalculateNewPoint =  (int x) => 
				{
					double y = m * x +b;
					if(PointValid(new Point((int)x,(int)y))){
						if(lastFoundNode == null)
						{
							RRTNode BetweenNode = new RRTNode(new Point((int)x,(int)y),Start.Orientation,Start); 
							Start.AddSucessor(BetweenNode);
							lastFoundNode = BetweenNode;
							BetweenNode.Inverted = End.Inverted;
							this.AllNodes.Add(BetweenNode);
						}
						else
						{
							RRTNode BetweenNode = new RRTNode(new Point((int)x,(int)y),  lastFoundNode.Orientation, lastFoundNode);
							lastFoundNode.AddSucessor(BetweenNode);
							lastFoundNode = BetweenNode;
							BetweenNode.Inverted = End.Inverted;
							this.AllNodes.Add(BetweenNode);
							//Console.WriteLine(BetweenNode.ToString());
						}
						return true;
					}
					else
						return false;
							
				};

				//Step with "StepWidth" from start x to end x (Or if the StartPosition is > then the EndPosition the other way round
				if (Start.Position.X < End.Position.X) {
					for (int x = Start.Position.X; x < End.Position.X; x += StepWidth) {
						if (!CalculateNewPoint (x)) //Break if a not valid point was stepped into
							break;
					}
				} else {
					for (int x = Start.Position.X; x > End.Position.X; x -= StepWidth) {
						if (!CalculateNewPoint (x))
							break;
					}
				}

			} else {

			}
		}
		/// <summary>
		/// Determins if the given point is valid
		/// </summary>
		/// <returns><c>true</c>, if valid was pointed, <c>false</c> otherwise.</returns>
		/// <param name="_Point">Point.</param>
		private bool PointValid(Point _Point)
		{
			return InternalMap.IsOccupied (_Point.X, _Point.Y);
		}

	}
}

