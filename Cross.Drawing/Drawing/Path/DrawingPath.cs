using System;

namespace Cross.Drawing
{
    /// <summary>
    /// Represents a collection of drawing commands that may be composed of arcs, curves, lines, and rectangles
    /// </summary>
    public class DrawingPath
    {
        /// <summary>
        /// Saving default capacity
        /// </summary>
        const int DefaultCapacity = 10;

        #region internal constants
        /// <summary>
        /// Indicate not using large arc.
        /// Instead using boolean to saving this attribute
        /// while add an curve to path,
        /// We using double const to saving this
        /// </summary>
        internal const double IsNotLargeArc = 0.0;
        /// <summary>
        /// Indicate using large arc.
        /// Instead using boolean to saving this attribute
        /// while add an curve to path,
        /// We using double const to saving this
        /// </summary>
        internal const double IsLargeArc = 1.0;
        /// <summary>
        /// Using right side arc.
        /// Instead using boolean to saving this attribute
        /// while add an curve to path,
        /// We using double const to saving this
        /// </summary>
        internal const double IsNotSweepLeftSide = 0.0;
        /// <summary>
        /// Using Left Side arc
        /// Instead using boolean to saving this attribute
        /// while add an curve to path,
        /// We using double const to saving this
        /// </summary>
        internal const double IsSweepLeftSide = 1.0;
        #endregion

        #region internal data
        /// <summary>
        /// saving commands, each command will saving path command type to this array
        /// </summary>
        internal DrawingPathCommand[] Commands = null;
        /// <summary>
        /// command count
        /// </summary>
        internal int CommandCount = 0;
        /// <summary>
        /// command capacity
        /// </summary>
        int CommandCapacity = 0;

        /// <summary>
        /// Coordinate for each command, to using this, must go from the beginning of path
        /// </summary>
        /// <remarks>
        /// Coordinate saving
        ///     MoveTo  : x,y
        ///     LineTo  : x,y
        ///     Bezier  : control point 1 X, control point 1 Y,
        ///               control point 2 X, control point 2 Y,
        ///               dest x, dest y
        ///     Quadratic bezier: control point X, control point Y
        ///                 dest X, dest y
        ///     Arc ( ellipse): radius X, radius Y, angle to x-axis,
        ///                 is large arc, is LeftSide arc, x, y
        /// </remarks>
        internal double[] Coordinates = null;

        /// <summary>
        /// coordinate count
        /// </summary>
        internal int CoordinateCount = 0;

        /// <summary>
        /// coordinate capacity
        /// </summary>
        int CoordinateCapacity = 0;

        /// <summary>
        /// current first x of polygon
        /// </summary>
        double currentFirstX = 0;

        /// <summary>
        /// current first y of polygon
        /// </summary>
        double currentFirstY = 0;

        /// <summary>
        /// Current index  of first command of polygon in command array
        /// </summary>
        int currentFirstCommandIndex = 0;

        /// <summary>
        /// Current index  of first command of polygon in coordinate array
        /// </summary>
        int currentFirstCoordinateIndex = 0;
        #endregion

        #region Constructor
        public DrawingPath()
        {
            CommandCapacity = DefaultCapacity;
            CommandCount = 0;
            Commands = new DrawingPathCommand[CommandCapacity];

            CoordinateCapacity = DefaultCapacity * 2;
            CoordinateCount = 0;
            Coordinates = new double[CoordinateCapacity];

            // always move to 0,0, as default
            Commands[CommandCount++] = DrawingPathCommand.NewFigure;
            Coordinates[CoordinateCount++] = 0;
            Coordinates[CoordinateCount++] = 0;
        }
        #endregion

        // private method
        #region Reserve Space
        /// <summary>
        /// Check if there is enough space for command and coordinate buffers. If not, allocate new space.
        /// </summary>
        private void ReserveSpace(int commandCount, int coordinateCount)
        {
            #region check if exceed command array or not
            commandCount--;
            if (CommandCount + commandCount >= CommandCapacity)
            {
                int newCapacity = CommandCapacity;
                if (commandCount > (CommandCapacity * 0.2)) newCapacity = CommandCapacity + commandCount;
                if (newCapacity < 1000) newCapacity = (int)(newCapacity * 2);
                else if (newCapacity < 5000) newCapacity = (int)(newCapacity * 1.5);
                else newCapacity = (int)(newCapacity * 1.2);

                DrawingPathCommand[] temp = new DrawingPathCommand[newCapacity];
                Commands.CopyTo(temp, 0);
                Commands = temp;
                CommandCapacity = newCapacity;
            }
            #endregion
            #region check if exceed coordinate array or not
            coordinateCount--;
            // coordinate count
            if (CoordinateCount + coordinateCount >= CoordinateCapacity)
            {
                int newCapacity = CoordinateCapacity;
                if (coordinateCount > (CoordinateCapacity * 0.2))
                {
                    newCapacity = CoordinateCapacity + coordinateCount;
                }
                if (newCapacity < 1000) newCapacity = (int)(newCapacity * 2);
                else if (newCapacity < 5000) newCapacity = (int)(newCapacity * 1.5);
                else newCapacity = (int)(newCapacity * 1.2);
                double[] temp = new double[newCapacity];
                Coordinates.CopyTo(temp, 0);
                Coordinates = temp;
                CoordinateCapacity = newCapacity;
            }
            #endregion
        }
        #endregion

        #region Move To
        /// <summary>
        /// Move current plotter to a new potision
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate ending point</param>
        public void MoveTo(double x, double y)
        {
            if (CommandCount > 1)
            {
                // reserve 1 command,and 2 coordinate
                ReserveSpace(1, 2);

                #region some special value for first move to
                currentFirstX = x;
                currentFirstY = y;
                currentFirstCommandIndex = CommandCount;
                currentFirstCoordinateIndex = CoordinateCount;
                #endregion

                Commands[CommandCount++] = DrawingPathCommand.NewFigure;
                Coordinates[CoordinateCount++] = x;
                Coordinates[CoordinateCount++] = y;
            }
            else
            {
                Commands[0] = DrawingPathCommand.NewFigure;
                Coordinates[0] = x;
                Coordinates[1] = y;
            }
        }
        #endregion

        #region Move By
        /// <summary>
        /// Move current plotter by a relative offsets to current position. Each MoveBy will create a new figure.
        /// </summary>
        /// <param name="offsetX">X-axis offset</param>
        /// <param name="offsetY">Y-axis offset</param>
        public void MoveBy(double offsetX, double offsetY)
        {
            if (CommandCount > 1)
            {
                offsetX += Coordinates[CoordinateCount - 2];
                offsetY += Coordinates[CoordinateCount - 1];

                // reserve 1 command,and 2 coordinate
                ReserveSpace(1, 2);

                #region some special value for first move to
                currentFirstX = offsetX;
                currentFirstY = offsetY;
                currentFirstCommandIndex = CommandCount;
                currentFirstCoordinateIndex = CoordinateCount;
                #endregion

                Commands[CommandCount++] = DrawingPathCommand.NewFigure;
                Coordinates[CoordinateCount++] = offsetX;
                Coordinates[CoordinateCount++] = offsetY;
            }
            else
            {
                Commands[0] = DrawingPathCommand.NewFigure;
                Coordinates[0] = offsetX;
                Coordinates[1] = offsetY;
            }
        }
        #endregion

        #region Line To
        /// <summary>
        /// Append a line from current position to new position (x, y) and move the plotter to new position
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate ending point</param>
        public void LineTo(double x, double y)
        {
            // reserve 1 command,and 2 coordinate
            ReserveSpace(1, 2);

            Commands[CommandCount++] = DrawingPathCommand.LineTo;
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }
        #endregion

        #region Line By
        /// <summary>
        /// Append a line from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="offsetX">x-axis offset to calculate the ending point</param>
        /// <param name="offsetY">Y-axis offset to calculate the ending point</param>
        public void LineBy(double offsetX, double offsetY)
        {
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];

            // reserve 1 command,and 2 coordinate
            ReserveSpace(1, 2);

            Commands[CommandCount++] = DrawingPathCommand.LineTo;
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }
        #endregion

        #region Add Line
        /// <summary>
        /// Move to (x1, y1) and draw a line to (x2, y2)
        /// </summary>
        /// <param name="x1">X-axis coordinate of starting point</param>
        /// <param name="y1">Y-axis coordinate of starting point</param>
        /// <param name="x2">X-axis coordinate of ending point</param>
        /// <param name="y2">Y-axis coordinate of ending point</param>
        public void AddLine(double x1, double y1, double x2, double y2)
        {
            // reserve 2 command,and 4 coordinate
            ReserveSpace(2, 4);

            // move to x1,y1
            Commands[CommandCount++] = DrawingPathCommand.MoveTo;
            Coordinates[CoordinateCount++] = x1;
            Coordinates[CoordinateCount++] = y1;

            // line to x2,y2
            Commands[CommandCount++] = DrawingPathCommand.LineTo;
            Coordinates[CoordinateCount++] = x2;
            Coordinates[CoordinateCount++] = y2;
        }
        #endregion

        #region Bezier

        #region Curve To
        /// <summary>
        /// Append a bezier curve from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="cp1X">X-axis coordinate of first control point</param>
        /// <param name="cp1Y">Y-axis coordinate of first control point</param>
        /// <param name="cp2X">X-axis coordinate of second control point</param>
        /// <param name="cp2Y">Y-axis coordinate of second control point</param>
        public void CurveTo(double x, double y, double cp1X, double cp1Y, double cp2X, double cp2Y)
        {
            // reserve 1 command,and 6 coordinate
            ReserveSpace(1, 6);

            Commands[CommandCount++] = DrawingPathCommand.CurveTo;
            // control point 1
            Coordinates[CoordinateCount++] = cp1X;
            Coordinates[CoordinateCount++] = cp1Y;
            // control point 2
            Coordinates[CoordinateCount++] = cp2X;
            Coordinates[CoordinateCount++] = cp2Y;
            // end point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }
        #endregion

        #region Curve By
        /// <summary>
        /// Append a bezier curve from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="offsetX">x-axis offset to calculate the ending point</param>
        /// <param name="offsetY">Y-axis offset to calculate the ending point</param>
        /// <param name="cp1X">X-axis coordinate of first control point</param>
        /// <param name="cp1Y">Y-axis coordinate of first control point</param>
        /// <param name="cp2X">X-axis coordinate of second control point</param>
        /// <param name="cp2Y">Y-axis coordinate of second control point</param>
        public void CurveBy(double offsetX, double offsetY, double cp1X, double cp1Y, double cp2X, double cp2Y)
        {
            // reserve 1 command,and 6 coordinate
            ReserveSpace(1, 6);

            #region change from relative to absolute coordinate
            double previousX = Coordinates[CoordinateCount - 2];
            double previousY = Coordinates[CoordinateCount - 1];
            // control point 1
            cp1X += previousX;
            cp1Y += previousY;

            // control point 2
            cp2X += previousX;
            cp2Y += previousY;

            // dest point
            offsetX += previousX;
            offsetY += previousY;
            #endregion

            Commands[CommandCount++] = DrawingPathCommand.CurveTo;
            // control point 1
            Coordinates[CoordinateCount++] = cp1X;
            Coordinates[CoordinateCount++] = cp1Y;
            // control point 2
            Coordinates[CoordinateCount++] = cp2X;
            Coordinates[CoordinateCount++] = cp2Y;
            // end point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }
        #endregion

        #region Add Curve
        /// <summary>
        /// Append a bezier curve from point(x1, y1) to (x2, y2)
        /// </summary>
        /// <param name="x1">X-axis coordinate of starting point</param>
        /// <param name="y1">Y-axis coordinate of starting point</param>
        /// <param name="cp1X">X-axis coordinate of first control point</param>
        /// <param name="cp1Y">Y-axis coordinate of first control point</param>
        /// <param name="cp2X">X-axis coordinate of second control point</param>
        /// <param name="cp2Y">Y-axis coordinate of second control point</param>
        /// <param name="x2">X-axis coordinate of ending point</param>
        /// <param name="y2">Y-axis coordinate of ending point</param>
        public void AddCurve(double x1, double y1, double cp1X, double cp1Y,
        double cp2X, double cp2Y, double x2, double y2)
        {

            // reserve 2 command,and 8 coordinate
            ReserveSpace(2, 8);

            // move to first
            Commands[CommandCount++] = DrawingPathCommand.MoveTo;
            // first point
            Coordinates[CoordinateCount++] = x1;
            Coordinates[CoordinateCount++] = y1;

            Commands[CommandCount++] = DrawingPathCommand.CurveTo;
            // control point 1
            Coordinates[CoordinateCount++] = cp1X;
            Coordinates[CoordinateCount++] = cp1Y;
            // control point 2
            Coordinates[CoordinateCount++] = cp2X;
            Coordinates[CoordinateCount++] = cp2Y;
            // end point
            Coordinates[CoordinateCount++] = x2;
            Coordinates[CoordinateCount++] = y2;
        }
        #endregion

        #region Smooth Curve To
        /// <summary>
        /// Append a bezier curve to from current position. The coordinate of the first controlling point is
        /// calculated automatically.
        /// </summary>
        /// <remarks>The first control point is assumed to be the reflection of second control point
        /// of previous curve ( or control point quadratic curve). When previous is not a curve,
        /// the control point is the last coordinate
        /// </remarks>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">X-axis coordinate of ending point</param>
        /// <param name="cp2X">X-axis coordinate of second control point</param>
        /// <param name="cp2Y">Y-axis coordinate of second control point</param>
        public void SmoothCurveTo(double x, double y, double cp2X, double cp2Y)
        {
            // smooth curve to
            // calculate first control point
            DrawingPathCommand lastCommand = Commands[CommandCount - 1];
            double firstControlPointX = 0.0;
            double firstControlPointY = 0.0;
            if ((lastCommand == DrawingPathCommand.CurveTo) || (lastCommand == DrawingPathCommand.QuadraticTo))
            {
                firstControlPointX = 2 * Coordinates[CoordinateCount - 2] - Coordinates[CoordinateCount - 4];
                firstControlPointY = 2 * Coordinates[CoordinateCount - 1] - Coordinates[CoordinateCount - 3];
            }
            else
            {
                firstControlPointX = Coordinates[CoordinateCount - 2];
                firstControlPointY = Coordinates[CoordinateCount - 1];
            }
            // curve to
            CurveTo(x, y, firstControlPointX, firstControlPointY, cp2X, cp2Y);
        }
        #endregion

        #region Smooth Curve By
        /// <summary>
        /// Append a bezier curve to from current position. The coordinate of the first controlling point is
        /// calculated automatically.
        /// </summary>
        /// <remarks>The first control point is assumed to be the reflection of second control point
        /// of previous curve ( or control point quadratic curve). When previous is not a curve,
        /// the control point is the last coordinate
        /// </remarks>
        /// <param name="offsetX">x-axis offset to calculate the ending point</param>
        /// <param name="offsetY">Y-axis offset to calculate the ending point</param>
        /// <param name="cp2X">X-axis coordinate of second control point</param>
        /// <param name="cp2Y">Y-axis coordinate of second control point</param>
        public void SmoothCurveBy(double offsetX, double offsetY, double cp2X, double cp2Y)
        {
            #region change from relative to absolute
            double previousX = Coordinates[CoordinateCount - 2];
            double previousY = Coordinates[CoordinateCount - 1];
            // control point
            cp2X += previousX;
            cp2Y += previousY;
            // dest point
            offsetX += previousX;
            offsetY += previousY;
            #endregion

            // smooth curve to
            // calculate first control point
            DrawingPathCommand lastCommand = Commands[CommandCount - 1];
            double firstControlPointX = 0.0;
            double firstControlPointY = 0.0;
            if ((lastCommand == DrawingPathCommand.CurveTo) || (lastCommand == DrawingPathCommand.QuadraticTo))
            {
                firstControlPointX = 2 * previousX - Coordinates[CoordinateCount - 4];
                firstControlPointY = 2 * previousY - Coordinates[CoordinateCount - 3];
            }
            else
            {
                firstControlPointX = previousX;
                firstControlPointY = previousY;
            }
            // curve to
            CurveTo(offsetX, offsetY, firstControlPointX, firstControlPointY, cp2X, cp2Y);
        }
        #endregion

        #endregion

        #region Quadratic bezier

        #region Quadratic Bezier To
        /// <summary>
        /// Append a quadratic bezier from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">X-axis coordinate of ending point</param>
        /// <param name="cpX">X-axis coordinate of control point</param>
        /// <param name="cpY">Y-axis coordinate of control point</param>
        public void QuadraticBezierTo(double x, double y, double cpX, double cpY)
        {
            // reserve 1 command,and 4 coordinate
            ReserveSpace(1, 4);

            Commands[CommandCount++] = DrawingPathCommand.QuadraticTo;

            // control point
            Coordinates[CoordinateCount++] = cpX;
            Coordinates[CoordinateCount++] = cpY;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;

        }
        #endregion

        #region Quadratic Bezier By
        /// <summary>
        /// Append a quadratic bezier from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="offsetX">x-axis offset to calculate the ending point</param>
        /// <param name="offsetY">Y-axis offset to calculate the ending point</param>
        /// <param name="cpX">X-axis coordinate of control point</param>
        /// <param name="cpY">Y-axis coordinate of control point</param>
        public void QuadraticBezierBy(double offsetX, double offsetY, double cpX, double cpY)
        {
            // reserve 1 command,and 4 coordinate
            ReserveSpace(1, 4);

            #region change from relative to absolute
            // control point
            cpX += Coordinates[CoordinateCount - 2];
            cpY += Coordinates[CoordinateCount - 1];

            // dest point
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion

            Commands[CommandCount++] = DrawingPathCommand.QuadraticTo;

            // control point
            Coordinates[CoordinateCount++] = cpX;
            Coordinates[CoordinateCount++] = cpY;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }
        #endregion

        #region Add Quadratic Bezier
        /// <summary>
        /// Append a quadratic bezier from (x1, y1) to (x2, y2)
        /// </summary>
        /// <param name="x1">X-axis coordinate of starting point</param>
        /// <param name="y1">Y-axis coordinate of starting point</param>
        /// <param name="cpX">X-axis coordinate of control point</param>
        /// <param name="cpY">Y-axis coordinate of control point</param>
        /// <param name="x2">X-axis coordinate of ending point</param>
        /// <param name="y2">Y-axis coordinate of ending point</param>
        public void AddQuadraticBezier(double x1, double y1, double cpX, double cpY, double x2, double y2)
        {
            // reserve 1 command,and 4 coordinate
            ReserveSpace(2, 6);

            Commands[CommandCount++] = DrawingPathCommand.MoveTo;
            Coordinates[CoordinateCount++] = x1;
            Coordinates[CoordinateCount++] = y1;

            Commands[CommandCount++] = DrawingPathCommand.QuadraticTo;

            // control point
            Coordinates[CoordinateCount++] = cpX;
            Coordinates[CoordinateCount++] = cpY;
            // dest point
            Coordinates[CoordinateCount++] = x2;
            Coordinates[CoordinateCount++] = y2;

        }
        #endregion

        #region Smooth Quadratic Bezier
        /// <summary>
        /// Append a quadratic bezier from current position to new position.
        /// The controlling point is calculated automatically.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">X-axis coordinate of ending point</param>
        public void SmoothQuadraticBezierTo(double x, double y)
        {
            // smooth curve to
            // calculate first control point
            DrawingPathCommand lastCommand = Commands[CommandCount - 1];
            double controlPointX = 0.0;
            double controlPointY = 0.0;
            if ((lastCommand == DrawingPathCommand.CurveTo) || (lastCommand == DrawingPathCommand.QuadraticTo))
            {
                // calculate reflect point
                controlPointX = 2 * Coordinates[CoordinateCount - 2] - Coordinates[CoordinateCount - 4];
                controlPointY = 2 * Coordinates[CoordinateCount - 1] - Coordinates[CoordinateCount - 3];
            }
            else
            {
                controlPointX = Coordinates[CoordinateCount - 2];
                controlPointY = Coordinates[CoordinateCount - 1];
            }
            // curve to
            QuadraticBezierTo(x, y, controlPointX, controlPointY);
        }
        #endregion

        #region Smooth Quadratic Bezier By
        /// <summary>
        /// Append a quadratic bezier from current position to new position.
        /// The new position is calculated by offseting from current position.
        /// The controlling point is calculated automatically.
        /// </summary>
        /// <param name="offsetX">x-axis offset to calculate the ending point</param>
        /// <param name="offsetY">Y-axis offset to calculate the ending point</param>
        public void SmoothQuadraticBezierBy(double offsetX, double offsetY)
        {
            // smooth curve to
            // calculate first control point
            DrawingPathCommand lastCommand = Commands[CommandCount - 1];
            double controlPointX = 0.0;
            double controlPointY = 0.0;
            if ((lastCommand == DrawingPathCommand.CurveTo) || (lastCommand == DrawingPathCommand.QuadraticTo))
            {
                // calculate reflect point
                controlPointX = 2 * Coordinates[CoordinateCount - 2] - Coordinates[CoordinateCount - 4];
                controlPointY = 2 * Coordinates[CoordinateCount - 1] - Coordinates[CoordinateCount - 3];
            }
            else
            {
                controlPointX = Coordinates[CoordinateCount - 2];
                controlPointY = Coordinates[CoordinateCount - 1];
            }
            // curve to
            QuadraticBezierTo(offsetX + Coordinates[CoordinateCount - 2], offsetY + Coordinates[CoordinateCount - 1], controlPointX, controlPointY);
        }
        #endregion
        #endregion

        #region Arc

        #region Arc To
        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radius">radius of both x-axis and y-axis</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcTo(double x, double y, double radius)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radius;
            Coordinates[CoordinateCount++] = radius;
            // angle
            Coordinates[CoordinateCount++] = 0;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcTo(double x, double y, double radiusX, double radiusY)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = 0;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcTo(double x, double y, double radiusX, double radiusY, double angle)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is using large arc or small arc</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcTo(double x, double y, double radiusX, double radiusY, double angle, bool isLargeArc)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is using large arc or small arc</param>
        /// <param name="isSweepLeftSide">is using LeftSide arc</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcTo(double x, double y, double radiusX, double radiusY, double angle, bool isLargeArc, bool isSweepLeftSide)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = isSweepLeftSide ? IsSweepLeftSide : IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x;
            Coordinates[CoordinateCount++] = y;
        }
        #endregion

        #region Arc By
        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radius">radius of arc</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcBy(double offsetX, double offsetY, double radius)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);

            #region change from relative to absolute
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radius;
            Coordinates[CoordinateCount++] = radius;
            // angle
            Coordinates[CoordinateCount++] = 0;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcBy(double offsetX, double offsetY, double radiusX, double radiusY)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);
            #region change from relative to absolute
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion
            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = 0;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcBy(double offsetX, double offsetY, double radiusX, double radiusY, double angle)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);
            #region change from relative to absolute
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion
            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is using large arc or small arc</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcBy(double offsetX, double offsetY, double radiusX, double radiusY, double angle, bool isLargeArc)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);
            #region change from relative to absolute
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion
            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }

        /// <summary>
        /// Append an arc from current position to new position and move the plotter to new position.
        /// The new position is calculated by offseting from current position.
        /// </summary>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is using large arc or small arc</param>
        /// <param name="isSweepLeftSide">is using LeftSide arc</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void ArcBy(double offsetX, double offsetY, double radiusX, double radiusY, double angle, bool isLargeArc, bool isSweepLeftSide)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(1, 7);
            #region change from relative to absolute
            offsetX += Coordinates[CoordinateCount - 2];
            offsetY += Coordinates[CoordinateCount - 1];
            #endregion
            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = isSweepLeftSide ? IsSweepLeftSide : IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = offsetX;
            Coordinates[CoordinateCount++] = offsetY;
        }
        #endregion

        #region Add Arc - Coordinate-based approach
        #region TO BE REMOVED - Hai Jan 29 2007
        /*
/// <summary>
/// Append an arc from (x1, y1) to (x2, y2)
/// </summary>
/// <param name="radius">radius</param>
/// <param name="x1">source x</param>
/// <param name="y1">source y</param>
/// <param name="x2">dest x</param>
/// <param name="y2">dest y</param>
/// <remarks>
/// Arc need following information
///     Radius X: radius x
///     Radius Y: radius y
///     Angle   : angle of ellipse base on x-axis ( default 0)
///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
/// </remarks>
public void AddArc(double x1,double y1,double radius, double x2, double y2)
{
// reserve 1 command,and 7 coordinate
ReserveSpace(2, 9);

#region move to first
Commands[CommandCount++] = DrawingPathCommand.MoveTo;
// saving source coordinate
Coordinates[CoordinateCount++] = x1;
Coordinates[CoordinateCount++] = y1;
#endregion

Commands[CommandCount++] = DrawingPathCommand.ArcTo;

// saving radius first
Coordinates[CoordinateCount++] = radius;
Coordinates[CoordinateCount++] = radius;
// angle
Coordinates[CoordinateCount++] = 0;
// is large arc or not
Coordinates[CoordinateCount++] = IsNotLargeArc;
// is sweep LeftSide or not
Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
// dest point
Coordinates[CoordinateCount++] = x2;
Coordinates[CoordinateCount++] = y2;
}

/// <summary>
/// Append an arc from (x1, y1) to (x2, y2)
/// </summary>
/// <param name="radiusX">radius x</param>
/// <param name="radiusY">radius y</param>
/// <param name="angle">angle to x-axis</param>
/// <param name="x">X-axis coordinate of ending point</param>
/// <param name="y">Y-axis coordinate of ending point</param>
/// <remarks>
/// Arc need following information
///     Radius X: radius x
///     Radius Y: radius y
///     Angle   : angle of ellipse base on x-axis ( default 0)
///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
/// </remarks>
public void AddArc(double x1,double y1,double radiusX, double radiusY, double x2, double y2)
{
// reserve 1 command,and 7 coordinate
ReserveSpace(2, 9);

#region move to first
Commands[CommandCount++] = DrawingPathCommand.MoveTo;
// saving source coordinate
Coordinates[CoordinateCount++] = x1;
Coordinates[CoordinateCount++] = y1;
#endregion

Commands[CommandCount++] = DrawingPathCommand.ArcTo;

// saving radius first
Coordinates[CoordinateCount++] = radiusX;
Coordinates[CoordinateCount++] = radiusY;
// angle
Coordinates[CoordinateCount++] = 0;
// is large arc or not
Coordinates[CoordinateCount++] = IsNotLargeArc;
// is sweep LeftSide or not
Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
// dest point
Coordinates[CoordinateCount++] = x2;
Coordinates[CoordinateCount++] = y2;
}

/// <summary>
/// Append an arc from (x1, y1) to (x2, y2)
/// </summary>
/// <param name="radiusX">radius x</param>
/// <param name="radiusY">radius y</param>
/// <param name="angle">angle to x-axis</param>
/// <param name="x">X-axis coordinate of ending point</param>
/// <param name="y">Y-axis coordinate of ending point</param>
/// <remarks>
/// Arc need following information
///     Radius X: radius x
///     Radius Y: radius y
///     Angle   : angle of ellipse base on x-axis ( default 0)
///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
/// </remarks>
public void AddArc(double x1,double y1,double radiusX, double radiusY, double angle, double x2, double y2)
{
// reserve 1 command,and 7 coordinate
ReserveSpace(2, 9);

#region move to first
Commands[CommandCount++] = DrawingPathCommand.MoveTo;
// saving source coordinate
Coordinates[CoordinateCount++] = x1;
Coordinates[CoordinateCount++] = y1;
#endregion

Commands[CommandCount++] = DrawingPathCommand.ArcTo;

// saving radius first
Coordinates[CoordinateCount++] = radiusX;
Coordinates[CoordinateCount++] = radiusY;
// angle
Coordinates[CoordinateCount++] = angle;
// is large arc or not
Coordinates[CoordinateCount++] = IsNotLargeArc;
// is sweep LeftSide or not
Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
// dest point
Coordinates[CoordinateCount++] = x2;
Coordinates[CoordinateCount++] = y2;
}

/// <summary>
/// Append an arc from (x1, y1) to (x2, y2)
/// </summary>
/// <param name="radiusX">radius x</param>
/// <param name="radiusY">radius y</param>
/// <param name="angle">angle to x-axis</param>
/// <param name="isLargeArc">is using large arc or small arc</param>
/// <param name="x">X-axis coordinate of ending point</param>
/// <param name="y">Y-axis coordinate of ending point</param>
/// <remarks>
/// Arc need following information
///     Radius X: radius x
///     Radius Y: radius y
///     Angle   : angle of ellipse base on x-axis ( default 0)
///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
/// </remarks>
public void AddArc(double x1,double y1,double radiusX, double radiusY, double angle, bool isLargeArc, double x2, double y2)
{
// reserve 1 command,and 7 coordinate
ReserveSpace(2, 9);

#region move to first
Commands[CommandCount++] = DrawingPathCommand.MoveTo;
// saving source coordinate
Coordinates[CoordinateCount++] = x1;
Coordinates[CoordinateCount++] = y1;
#endregion

Commands[CommandCount++] = DrawingPathCommand.ArcTo;

// saving radius first
Coordinates[CoordinateCount++] = radiusX;
Coordinates[CoordinateCount++] = radiusY;
// angle
Coordinates[CoordinateCount++] = angle;
// is large arc or not
Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
// is sweep LeftSide or not
Coordinates[CoordinateCount++] = IsNotSweepLeftSide;
// dest point
Coordinates[CoordinateCount++] = x2;
Coordinates[CoordinateCount++] = y2;
}
*/
        #endregion

        /// <summary>
        /// Append an arc from (x1, y1) to (x2, y2)
        /// </summary>
        /// <param name="radiusX">radius x</param>
        /// <param name="radiusY">radius y</param>
        /// <param name="angle">angle to x-axis</param>
        /// <param name="isLargeArc">is using large arc or small arc</param>
        /// <param name="isSweepLeftSide">is using LeftSide arc</param>
        /// <param name="x">X-axis coordinate of ending point</param>
        /// <param name="y">Y-axis coordinate of ending point</param>
        /// <remarks>
        /// Arc need following information
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse base on x-axis ( default 0)
        ///     IsLagreArc  : Is draw large arc (default false, mean 0.0 in coordinate array)
        ///     IsSweepLeftSide: is sweep LeftSide ellipse ( default false,mean 0.0 in cooridnate array)
        /// </remarks>
        public void AddArc(double x1, double y1, double x2, double y2, double radiusX, double radiusY, double angle, bool isLargeArc, bool isSweepLeftSide)
        {
            // reserve 1 command,and 7 coordinate
            ReserveSpace(2, 9);

            #region move to first
            Commands[CommandCount++] = DrawingPathCommand.MoveTo;
            // saving source coordinate
            Coordinates[CoordinateCount++] = x1;
            Coordinates[CoordinateCount++] = y1;
            #endregion

            Commands[CommandCount++] = DrawingPathCommand.ArcTo;

            // saving radius first
            Coordinates[CoordinateCount++] = radiusX;
            Coordinates[CoordinateCount++] = radiusY;
            // angle
            Coordinates[CoordinateCount++] = angle;
            // is large arc or not
            Coordinates[CoordinateCount++] = isLargeArc ? IsLargeArc : IsNotLargeArc;
            // is sweep LeftSide or not
            Coordinates[CoordinateCount++] = isSweepLeftSide ? IsSweepLeftSide : IsNotSweepLeftSide;
            // dest point
            Coordinates[CoordinateCount++] = x2;
            Coordinates[CoordinateCount++] = y2;
        }
        #endregion

        #region Add Arc - Angle-based approach
        /// <summary>
        /// Append an arc from starting and ending angle
        /// </summary>
        /// <param name="cx">X-axis coordinate of center point</param>
        /// <param name="cy">Y-axis coordinate of center point</param>
        /// <param name="radiusX">X-axis radius</param>
        /// <param name="radiusY">Y-axis radius</param>
        /// <param name="startAngle">Starting angle</param>
        /// <param name="endAngle">Ending angle</param>
        public void AddArc(double cx, double cy, double radiusX, double radiusY, double startAngle, double endAngle)
        {

            //if start angle - endangle > Math.Pi * 2;
            if ((startAngle - endAngle) > 360)
            {
                endAngle = 360 + endAngle;
            }
            else
            {
                if ((endAngle - startAngle) > 360)
                {
                    endAngle = -360 + endAngle;
                }
            }
            //convert angles to radian
            startAngle = Math.PI * startAngle / 180.0;
            endAngle = Math.PI * endAngle / 180.0;

            //calculate start point, end point
            double x1 = cx + radiusX * Math.Cos(startAngle);
            double y1 = cy - radiusY * Math.Sin(startAngle);
            double x2 = cx + radiusX * Math.Cos(endAngle);
            double y2 = cy - radiusY * Math.Sin(endAngle);

            //determine largeArc, sweep Left
            bool isLargeArc = false;
            if (Math.Abs(startAngle - endAngle) > Math.PI)
            {
                isLargeArc = true;
            }
            bool isSweepLeft = true;
            //
            if (startAngle < endAngle)
                isSweepLeft = false;

            //append data

            AddArc(x1, y1, x2, y2, radiusX, radiusY, 0, isLargeArc, isSweepLeft);
        }
        #endregion

        #endregion

        #region Add path
        /// <summary>
        /// Append a path to current path
        /// </summary>
        /// <param name="path">The source path to copy from</param>
        public void AddPath(DrawingPath path)
        {
            // using array .copy only
            ReserveSpace(path.CommandCount, path.CoordinateCount);

            // change private fields
            currentFirstCommandIndex = CommandCount + path.currentFirstCommandIndex;
            currentFirstCoordinateIndex = CoordinateCount + path.currentFirstCoordinateIndex;
            currentFirstX = path.currentFirstX;
            currentFirstY = path.currentFirstY;

            // copy command
            Array.Copy(path.Commands, 0, Commands, CommandCount, path.CommandCount);
            CommandCount += path.CommandCount;
            // copy coordinates
            Array.Copy(path.Coordinates, 0, Coordinates, CoordinateCount, path.CoordinateCount);
            CoordinateCount += path.CoordinateCount;
        }

        /// <summary>
        /// Add a path to current path by transforming data from source path
        /// NOTES: Currently this method does not support Arc commands from source path
        /// </summary>
        /// <param name="path">The source path to copy from</param>
        public void AddPath(DrawingPath path, Matrix3x3 matrix)
        {
            // using array .copy only
            ReserveSpace(path.CommandCount, path.CoordinateCount);

            // change private fields
            currentFirstCommandIndex = CommandCount + path.currentFirstCommandIndex;
            currentFirstCoordinateIndex = CoordinateCount + path.currentFirstCoordinateIndex;
            currentFirstX = path.currentFirstX;
            currentFirstY = path.currentFirstY;

            // copy command
            Array.Copy(path.Commands, 0, Commands, CommandCount, path.CommandCount);
            CommandCount += path.CommandCount;
            // copy coordinates
            //Array.Copy(path.Coordinates, 0, Coordinates, CoordinateCount, path.CoordinateCount);
            //CoordinateCount += path.CoordinateCount;
            double sx = matrix.Sx;
            double sy = matrix.Sy;
            double shx = matrix.Shx;
            double shy = matrix.Shy;
            double tx = matrix.Tx;
            double ty = matrix.Ty;

            //double tmp = x;
            //x = tmp * Sx + y * Shx + Tx;
            //y = tmp * Shy + y * Sy + Ty;
            double[] pathCoodinates = path.Coordinates;
            for (int cooridnateIndex = 0; cooridnateIndex < path.CoordinateCount; cooridnateIndex += 2)
            {
                Coordinates[CoordinateCount++] =
                pathCoodinates[cooridnateIndex] * sx
                + pathCoodinates[cooridnateIndex + 1] * shx + tx;
                Coordinates[CoordinateCount++] =
                pathCoodinates[cooridnateIndex] * shy
                + pathCoodinates[cooridnateIndex + 1] * sy + ty;
            }

        }
        #endregion

        #region Close current figure
        /// <summary>
        /// Close current figure
        /// </summary>
        public void CloseFigure()
        {
            int fromCheck = currentFirstCommandIndex;
            while (fromCheck < CommandCount)
            {
                if (Commands[fromCheck] == DrawingPathCommand.MoveTo)
                {
                    Commands[fromCheck] = DrawingPathCommand.LineTo;
                }
                fromCheck++;
            }
            if (!((Coordinates[currentFirstCoordinateIndex] == Coordinates[CoordinateCount - 2])
            && (Coordinates[currentFirstCoordinateIndex + 1] == Coordinates[CoordinateCount - 1])))
            {
                // add line to command to current figure
                LineTo(Coordinates[currentFirstCoordinateIndex], Coordinates[currentFirstCoordinateIndex + 1]);
            }
        }
        #endregion

        #region Close all figures
        /// <summary>
        /// Close all figures in path.
        /// </summary>
        public void CloseAllFigures()
        {
            bool isFirst = true;
            for (int command = 0; command < CommandCount; command++)
            {
                if (Commands[command] == DrawingPathCommand.NewFigure)
                {
                    if (isFirst)
                    {
                        // is first of all
                        isFirst = false;
                    }
                    else
                    {
                        Commands[command] = DrawingPathCommand.NewFigureAndCloseLast;
                    }
                }
                else if (Commands[command] == DrawingPathCommand.MoveTo)
                {
                    Commands[command] = DrawingPathCommand.LineTo;
                }
            }
        }
        #endregion
    }
}

