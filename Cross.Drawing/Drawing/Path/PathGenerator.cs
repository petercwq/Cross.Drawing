#region Using directives
using System;

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Internal used for generate from path data to polygon raw data
    /// </summary>
    internal class DrawingPathGenerator
    {
        #region const
        /// <summary>
        /// Two pi
        /// </summary>
        const double TwoPi = 2.0 * Math.PI;

        /// <summary>
        /// Change from degree to radian
        /// </summary>
        const double DegreeToRadian = Math.PI / 180.0;

        /// <summary>
        /// Small step angle
        /// </summary>
        const double SmallStepAngle = 1.570796;

        /// <summary>
        /// Small step angle negative
        /// </summary>
        const double SmallStepAngleNegative = -1.570796;

        /// <summary>
        /// Const to change from distance to number of step interpolate
        /// when generate arc,curve ..
        /// </summary>
        const double DistanceToNumberOfStepScale = 0.25;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for PathGenerator
        /// </summary>
        public DrawingPathGenerator()
        { }
        #endregion

        #region generate
        /// <summary>
        /// Generate polygon from path
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="averageScale">average scale of transform, this
        /// to decrease number of point generate for curve ...</param>
        /// <returns></returns>
        public double[][] Generate(DrawingPath path, double averageScale)
        {
            RawPolygonList polygonList = new RawPolygonList();

            DrawingPathCommand[] commands = path.Commands;
            double[] coordinates = path.Coordinates;
            int commandCount = path.CommandCount;
            int coordinateCount = path.CoordinateCount;
            int coordinateIndex = 0;
            //foreach(PathCommand command in commands)
            for (int commandIndex = 0; commandIndex < commandCount; commandIndex++)
            {
                switch (commands[commandIndex])
                {
                    case DrawingPathCommand.MoveTo:
                    case DrawingPathCommand.NewFigure:
                        #region move to
                        if (path.Commands[commandIndex + 1] == DrawingPathCommand.ArcTo)
                        {
                            coordinateIndex += 2;
                            break;
                        }
                        polygonList.MoveTo(coordinates[coordinateIndex++], coordinates[coordinateIndex++]);
                        break;
                        #endregion
                    case DrawingPathCommand.NewFigureAndCloseLast:
                        #region close last and new figure
                        // close last
                        polygonList.CloseCurrentPolygon();
                        // move to
                        polygonList.MoveTo(coordinates[coordinateIndex++], coordinates[coordinateIndex++]);
                        break;
                        #endregion
                    case DrawingPathCommand.LineTo:
                        #region line to
                        polygonList.LineTo(coordinates[coordinateIndex++], coordinates[coordinateIndex++]);
                        #endregion
                        break;
                    case DrawingPathCommand.ArcTo:
                        #region arc to
                        BuildArc(polygonList,
                            coordinates[coordinateIndex - 2], // start x
                            coordinates[coordinateIndex - 1], // start y
                            coordinates[coordinateIndex++], // radius x
                            coordinates[coordinateIndex++], // radius y
                            coordinates[coordinateIndex++], // angle
                            coordinates[coordinateIndex++] == DrawingPath.IsLargeArc,      // is large arc
                            coordinates[coordinateIndex++] == DrawingPath.IsSweepLeftSide, // is sweep left side
                            coordinates[coordinateIndex++], // dest x
                            coordinates[coordinateIndex++], // dest y
                            averageScale);
                        #endregion

                        break;

                    case DrawingPathCommand.CurveTo:
                        #region curve to
                        BuildCurve(polygonList,
                            coordinates[coordinateIndex - 2],   // start x
                            coordinates[coordinateIndex - 1],   // start y
                            coordinates[coordinateIndex++],     // control point 1 x
                            coordinates[coordinateIndex++],     // control point 1 y
                            coordinates[coordinateIndex++],     // control point 2 x
                            coordinates[coordinateIndex++],     // control point 2 y
                            coordinates[coordinateIndex++],     // end x
                            coordinates[coordinateIndex++],     // end y
                            averageScale);
                        #endregion
                        break;

                    case DrawingPathCommand.QuadraticTo:
                        #region quadratic bezier
                        BuildQuadratic(polygonList,
                            coordinates[coordinateIndex - 2],   // start x
                            coordinates[coordinateIndex - 1],   // start y
                            coordinates[coordinateIndex++],     // control point x
                            coordinates[coordinateIndex++],     // control point y
                            coordinates[coordinateIndex++],     // end x
                            coordinates[coordinateIndex++],     // end y
                            averageScale);
                        #endregion
                        break;
                }
            }

            // finish generation
            polygonList.Finish();
            return polygonList.RawDatas;
        }
        #endregion

        #region build curve
        /// <summary>
        /// build curve and append to polygon list
        /// </summary>
        /// <param name="polygonList">polygon list</param>
        /// <param name="x1">x1</param>
        /// <param name="y1">y1</param>
        /// <param name="c1x">control point 1 x</param>
        /// <param name="c1y">control point 1 y</param>
        /// <param name="c2x">control point 2 x</param>
        /// <param name="c2y">control point 2 y</param>
        /// <param name="x2">x2</param>
        /// <param name="y2">y2</param>
        /// <param name="scale"></param>
        private void BuildCurve(RawPolygonList polygonList,
            double x1, double y1, double c1x, double c1y,
            double c2x, double c2y, double x2, double y2,
            double scale)
        {

            #region calculate number of step
            double lengthOfCurve = Math.Sqrt((c1x - x1) * (c1x - x1) + (c1y - y1) * (c1y - y1))
                                 + Math.Sqrt((c2x - c1x) * (c2x - c1x) + (c2y - c1y) * (c2y - c1y))
                                 + Math.Sqrt((x2 - c2x) * (x2 - c2x) + (y2 - c2y) * (y2 - c2y));

            double mSteps = lengthOfCurve * DistanceToNumberOfStepScale * scale;
            #endregion

            #region add next point to polygon

            int i = 0;
            //double resultX, resultY, mu;
            double mu;
            double restOfMu;
            //add next point to polygon
            while (i < mSteps)
            {
                #region calculate point[i]
                mu = i / mSteps;
                restOfMu = 1 - mu;
                //add point [i] to polygon
                polygonList.LineTo
                    (
                        x1 * restOfMu * restOfMu * restOfMu +
                        3 * c1x * mu * restOfMu * restOfMu +
                        3 * c2x * mu * mu * restOfMu
                        + x2 * mu * mu * mu,

                        y1 * restOfMu * restOfMu * restOfMu +
                        3 * c1y * mu * restOfMu * restOfMu +
                        3 * c2y * mu * mu * restOfMu
                        + y2 * mu * mu * mu
                    );
                #endregion
                i++;
            }
            #endregion
            polygonList.LineTo(x2, y2);

        }
        #endregion

        #region build arc
        /// <summary>
        /// Build an arc to polygon list
        /// </summary>
        /// <param name="polygonList">polygon list</param>
        /// <param name="xStart"> start x</param>
        /// <param name="yStart"> start y</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is draw large arc</param>
        /// <param name="isSweepLeftSide">is draw left side arc</param>
        /// <param name="xEnd">end x</param>
        /// <param name="yEnd">end y</param>
        /// <param name="scale">scale</param>
        /// 
        private void BuildArc(RawPolygonList polygonList,
            double xStart, double yStart,
            double radiusX, double radiusY,
            double angle, bool isLargeArc, bool isSweepLeftSide,
            double xEnd, double yEnd, double scale)
        {
            // when start is end
            if (xStart == xEnd && yStart == yEnd)
            {
                // there are 4 ellipses and need to determine which will be draw
                throw new NotImplementedException("Not implemented");
            }
            else
            {
                double centerX, centerY, startAngle, sweepAngle;

                #region calculate to center point , start angle and sweep angle

                if (radiusX < 0.0) radiusX = -radiusX;
                if (radiusY < 0.0) radiusY = -radiusX;

                // Calculate the middle point between 
                // the current and the final points
                //------------------------
                double dx2 = (xStart - xEnd) / 2.0;
                double dy2 = (yStart - yEnd) / 2.0;

                angle = angle * DegreeToRadian;

                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                // Calculate (x1, y1)
                //------------------------
                double x1 = cosAngle * dx2 + sinAngle * dy2;
                double y1 = -sinAngle * dx2 + cosAngle * dy2;

                // Ensure radii are large enough
                //------------------------
                double prx = radiusX * radiusX;
                double pry = radiusY * radiusY;
                double px1 = x1 * x1;
                double py1 = y1 * y1;

                // Check that radii are large enough
                //------------------------
                double radii_check = px1 / prx + py1 / pry;
                if (radii_check > 1.0)
                {
                    radiusX = Math.Sqrt(radii_check) * radiusX;
                    radiusY = Math.Sqrt(radii_check) * radiusY;
                    prx = radiusX * radiusX;
                    pry = radiusY * radiusY;
                }

                // Calculate (cx1, cy1)
                //------------------------
                double sign = (isLargeArc == isSweepLeftSide) ? -1.0 : 1.0;
                double sq = (prx * pry - prx * py1 - pry * px1) / (prx * py1 + pry * px1);
                double coef = sign * Math.Sqrt((sq < 0) ? 0 : sq);
                double cx1 = coef * ((radiusX * y1) / radiusY);
                double cy1 = coef * -((radiusY * x1) / radiusX);
                //
                // Calculate (cx, cy) from (cx1, cy1)
                //------------------------
                double sx2 = (xStart + xEnd) / 2.0;
                double sy2 = (yStart + yEnd) / 2.0;
                centerX = sx2 + (cosAngle * cx1 - sinAngle * cy1);
                centerY = sy2 + (sinAngle * cx1 + cosAngle * cy1);

                // Calculate the start_angle (angle1) and the sweep_angle (dangle)
                //------------------------
                double ux = (x1 - cx1) / radiusX;
                double uy = (y1 - cy1) / radiusY;
                double vx = (-x1 - cx1) / radiusX;
                double vy = (-y1 - cy1) / radiusY;
                double p, n;

                // Calculate the angle start
                //------------------------
                n = Math.Sqrt(ux * ux + uy * uy);
                p = ux; // (1 * ux) + (0 * uy)
                sign = (uy < 0) ? -1.0 : 1.0;
                double v = p / n;
                if (v < -1.0) v = -1.0;
                else if (v > 1.0) v = 1.0;
                startAngle = sign * Math.Acos(v);

                // Calculate the sweep angle
                //------------------------
                n = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
                p = ux * vx + uy * vy;
                sign = (ux * vy - uy * vx < 0) ? -1.0 : 1.0;
                v = p / n;
                if (v < -1.0) v = -1.0;
                else if (v > 1.0) v = 1.0;
                sweepAngle = sign * Math.Acos(v);
                if (!isSweepLeftSide && sweepAngle > 0)
                {
                    sweepAngle -= TwoPi;
                }
                else
                    if (isSweepLeftSide && sweepAngle < 0)
                    {
                        sweepAngle += TwoPi;
                    }
                #endregion


                // We can now build and transform the resulting arc
                //------------------------           
                BuildBezierArc(polygonList,
                    centerX, centerY,
                    radiusX, radiusY,
                    angle, startAngle, sweepAngle,
                    scale);
            }

        }




        #region build to bezier arc
        /// <summary>
        /// build arc to beziers 
        /// </summary>
        /// <param name="polygonList">polygon List</param>
        /// <param name="centerX">coordinate X of center point</param>
        /// <param name="centerY">coordinate Y of center point</param>
        /// <param name="radiusX">radius X</param>
        /// <param name="radiusY">radius Y</param>
        /// <param name="angle">angle of arc</param>
        /// <param name="startAngle">angle start sweep</param>
        /// <param name="sweepAngle">angle sweep</param>
        /// <param name="scale">scale</param>
        private void BuildBezierArc(RawPolygonList polygonList, double centerX, double centerY, double radiusX, double radiusY, double angle, double startAngle, double sweepAngle, double scale)
        {
            #region variable
            int numberVertices;
            double tempCenterX = centerX;
            double tempCenterY = centerY;
            centerX = centerY = 0.0;
            double[] vertices = new double[26];
            #endregion

            #region recalculate start angle and sweep angle
            startAngle = startAngle % TwoPi;

            if (sweepAngle > TwoPi) sweepAngle = TwoPi;
            else if (sweepAngle < -TwoPi) sweepAngle = -TwoPi;
            #endregion

            #region if sweep angle < 1e -10 then cannot draw arc
            if (Math.Abs(sweepAngle) < 1e-10)
            {
                numberVertices = 4;
                polygonList.LineTo(
                    centerX + radiusX * Math.Cos(startAngle),
                    centerY + radiusY * Math.Sin(startAngle));
                polygonList.LineTo(
                    centerX + radiusX * Math.Cos(startAngle + sweepAngle)
                    , centerY + radiusY * Math.Sin(startAngle + sweepAngle));
                return;
            }
            #endregion

            #region calculate all control point in bezier
            double totalSweep = 0.0, localSweep = 0.0;
            double prevSweep;
            numberVertices = 2;
            double x0, y0, tx, ty, sn, cs, tempX;
            bool done = false;

            #region when sweep angle less than 0
            if (sweepAngle > 0)
            {

                do
                {
                    prevSweep = totalSweep;
                    localSweep = SmallStepAngle;
                    totalSweep += localSweep;
                    if (totalSweep >= sweepAngle - 0.01)
                    {
                        localSweep = sweepAngle - prevSweep;
                        done = true;
                    }

                    #region create new control points in beziers

                    x0 = Math.Cos(localSweep / 2.0);
                    y0 = Math.Sin(localSweep / 2.0);
                    tx = (1.0 - x0) * 4.0 / 3.0;
                    ty = y0 - tx * x0 / y0;
                    tempX = x0 + tx;

                    //calculate sin and code of middle angle
                    sn = Math.Sin(startAngle + localSweep / 2.0);
                    cs = Math.Cos(startAngle + localSweep / 2.0);

                    vertices[numberVertices - 2] = radiusX * (x0 * cs + y0 * sn);
                    vertices[numberVertices - 1] = radiusY * (x0 * sn - y0 * cs);
                    vertices[numberVertices] = radiusX * (tempX * cs + ty * sn);
                    vertices[numberVertices + 1] = radiusY * (tempX * sn - ty * cs);
                    vertices[numberVertices + 2] = radiusX * (tempX * cs - ty * sn);
                    vertices[numberVertices + 3] = radiusY * (tempX * sn + ty * cs);
                    vertices[numberVertices + 4] = radiusX * (x0 * cs - y0 * sn);
                    vertices[numberVertices + 5] = radiusY * (x0 * sn + y0 * cs);
                    #endregion


                    numberVertices += 6;
                    startAngle += localSweep;
                }
                while (!done && numberVertices < 26);
            }
            #endregion

            #region when negative sweep angle
            else
            {
                do
                {
                    prevSweep = totalSweep;
                    localSweep = SmallStepAngleNegative;
                    totalSweep += localSweep;
                    if (totalSweep <= sweepAngle + 0.01)
                    {
                        localSweep = sweepAngle - prevSweep;
                        done = true;
                    }

                    #region create new control points in beziers
                    x0 = Math.Cos(localSweep / 2.0);
                    y0 = Math.Sin(localSweep / 2.0);
                    tx = (1.0 - x0) * 4.0 / 3.0;
                    ty = y0 - tx * x0 / y0;
                    tempX = x0 + tx;

                    //calculate sin and code of middle angle
                    sn = Math.Sin(startAngle + localSweep / 2.0);
                    cs = Math.Cos(startAngle + localSweep / 2.0);

                    vertices[numberVertices - 2] = radiusX * (x0 * cs + y0 * sn);
                    vertices[numberVertices - 1] = radiusY * (x0 * sn - y0 * cs);
                    vertices[numberVertices] = radiusX * (tempX * cs + ty * sn);
                    vertices[numberVertices + 1] = radiusY * (tempX * sn - ty * cs);
                    vertices[numberVertices + 2] = radiusX * (tempX * cs - ty * sn);
                    vertices[numberVertices + 3] = radiusY * (tempX * sn + ty * cs);
                    vertices[numberVertices + 4] = radiusX * (x0 * cs - y0 * sn);
                    vertices[numberVertices + 5] = radiusY * (x0 * sn + y0 * cs);
                    #endregion


                    numberVertices += 6;
                    startAngle += localSweep;
                }
                while (!done && numberVertices < 26);
            }
            #endregion


            #endregion

            #region generate points from control points of bezier curves
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            int i = 0;
            polygonList.MoveTo(vertices[0] * cosAngle - vertices[1] * sinAngle + tempCenterX, vertices[0] * sinAngle + vertices[1] * cosAngle + tempCenterY);
            while (i + 7 < numberVertices)
            {
                BuildCurve(polygonList,
                      vertices[i + 0] * cosAngle - vertices[i + 1] * sinAngle + tempCenterX, vertices[i + 0] * sinAngle + vertices[i + 1] * cosAngle + tempCenterY,
                      vertices[i + 2] * cosAngle - vertices[i + 3] * sinAngle + tempCenterX, vertices[i + 2] * sinAngle + vertices[i + 3] * cosAngle + tempCenterY,
                      vertices[i + 4] * cosAngle - vertices[i + 5] * sinAngle + tempCenterX, vertices[i + 4] * sinAngle + vertices[i + 5] * cosAngle + tempCenterY,
                      vertices[i + 6] * cosAngle - vertices[i + 7] * sinAngle + tempCenterX, vertices[i + 6] * sinAngle + vertices[i + 7] * cosAngle + tempCenterY,
                      scale);
                i += 6;
            }
            #endregion
        }
        #endregion
        #endregion

        #region build quadratic bezier
        /// <summary>
        /// Build quadratic bezier
        /// </summary>
        /// <param name="x1">start x</param>
        /// <param name="y1">start y</param>
        /// <param name="cx">control point x</param>
        /// <param name="cy">control point y</param>
        /// <param name="x2">end x</param>
        /// <param name="y2">end y</param>
        private void BuildQuadratic(
            RawPolygonList polygonList,
            double x1, double y1,
            double cx, double cy,
            double x2, double y2, double scale)
        {
            #region calculate number of step
            double lengthOfCurve = Math.Sqrt((cx - x1) * (cx - x1) + (cy - y1) * (cy - y1))
                                 + Math.Sqrt((x2 - cx) * (x2 - cx) + (y2 - cy) * (y2 - cy));
            double mSteps = lengthOfCurve * DistanceToNumberOfStepScale * scale;
            int i = 0;
            double mu;
            while (i < mSteps)
            {

                #region calculate point[i]
                mu = i / mSteps;
                //add point [i] to polygon
                polygonList.LineTo(
                    x1 * (1 - mu) * (1 - mu) + 2 * cx * mu * (1 - mu) + x2 * mu * mu,
                    y1 * (1 - mu) * (1 - mu) + 2 * cy * mu * (1 - mu) + y2 * mu * mu
                    );
                #endregion
                i++;
            }
            #endregion

            polygonList.LineTo(x2, y2);
        }
        #endregion
    }
}
