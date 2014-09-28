
namespace Cross.Drawing
{
    /// <summary>
    /// Path command using internal to determine current coordinate
    /// </summary>
    internal enum DrawingPathCommand
    {
        /// <summary>
        /// Special move to, when call direct xy move to in path object
        /// </summary>
        NewFigure,
        /// <summary>
        /// Special move to, this will implemented while using closeAllFigures
        /// </summary>
        NewFigureAndCloseLast,
        /// <summary>
        /// Move to new coordinate, this will create new polygon in final result
        /// </summary>
        MoveTo,
        /// <summary>
        /// Add a line to final polygon
        /// </summary>
        LineTo,
        /// <summary>
        /// Add an ellipse arc to final polygon.
        /// This will include more information:
        ///     Radius X: radius x
        ///     Radius Y: radius y
        ///     Angle   : angle of ellipse
        ///     IsLagreArc  : Is draw large arc
        ///     IsSweepUpper: is sweep upper ellipse
        /// See picture in page 174, SVG specs
        /// </summary>
        ArcTo,
        /// <summary>
        /// Add an bezier curve to final polygon. This need more information
        /// about two middle control points: x1,y1; x2,y2
        /// </summary>
        CurveTo,
        /// <summary>
        /// Add quadratic,a type of bezier including 1 control point only
        /// </summary>
        QuadraticTo

    }
}

