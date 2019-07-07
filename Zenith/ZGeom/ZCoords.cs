using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;
using Zenith.ZMath;
using static Zenith.ZMath.CubeSector;

namespace Zenith.ZGeom
{
    // class to document all coordinate stuff in the form of code, instead of just writing it down
    public static class ZCoords
    {
        public static ISectorManager GetSectorManager()
        {
            return new CubeSectorManager();
        }

        public static string GetFaceAcronym(this CubeSectorFace sectorFace)
        {
            switch (sectorFace)
            {
                case CubeSectorFace.FRONT:
                    return "FR";
                case CubeSectorFace.BACK:
                    return "BA";
                case CubeSectorFace.LEFT:
                    return "LE";
                case CubeSectorFace.RIGHT:
                    return "RI";
                case CubeSectorFace.TOP:
                    return "TO";
                case CubeSectorFace.BOTTOM:
                    return "BO";
            }
            throw new NotImplementedException();
        }

        public static CubeSectorFace FromFaceAcronym(string s)
        {
            switch (s)
            {
                case "FR":
                    return CubeSectorFace.FRONT;
                case "BA":
                    return CubeSectorFace.BACK;
                case "LE":
                    return CubeSectorFace.LEFT;
                case "RI":
                    return CubeSectorFace.RIGHT;
                case "TO":
                    return CubeSectorFace.TOP;
                case "BO":
                    return CubeSectorFace.BOTTOM;
            }
            throw new NotImplementedException();
        }

        public static Vector3d GetFaceNormal(this CubeSectorFace sectorFace)
        {
            switch (sectorFace)
            {
                case CubeSectorFace.FRONT:
                    return new Vector3d(0, -1, 0);
                case CubeSectorFace.BACK:
                    return new Vector3d(0, 1, 0);
                case CubeSectorFace.LEFT:
                    return new Vector3d(-1, 0, 0);
                case CubeSectorFace.RIGHT:
                    return new Vector3d(1, 0, 0);
                case CubeSectorFace.TOP:
                    return new Vector3d(0, 0, 1);
                case CubeSectorFace.BOTTOM:
                    return new Vector3d(0, 0, -1);
            }
            throw new NotImplementedException();
        }

        // up as in the the top of an image, for instance
        public static Vector3d GetFaceUpDirection(this CubeSectorFace sectorFace)
        {
            switch (sectorFace)
            {
                case CubeSectorFace.FRONT:
                    return GetFaceNormal(CubeSectorFace.TOP);
                case CubeSectorFace.BACK:
                    return GetFaceNormal(CubeSectorFace.TOP);
                case CubeSectorFace.LEFT:
                    return GetFaceNormal(CubeSectorFace.TOP);
                case CubeSectorFace.RIGHT:
                    return GetFaceNormal(CubeSectorFace.TOP);
                case CubeSectorFace.TOP:
                    return GetFaceNormal(CubeSectorFace.BACK);
                case CubeSectorFace.BOTTOM:
                    return GetFaceNormal(CubeSectorFace.BACK);
            }
            throw new NotImplementedException();
        }

        public static Vector3d GetFaceRightDirection(this CubeSectorFace sectorFace)
        {
            switch (sectorFace)
            {
                case CubeSectorFace.FRONT:
                    return GetFaceNormal(CubeSectorFace.RIGHT);
                case CubeSectorFace.BACK:
                    return GetFaceNormal(CubeSectorFace.LEFT);
                case CubeSectorFace.LEFT:
                    return GetFaceNormal(CubeSectorFace.FRONT);
                case CubeSectorFace.RIGHT:
                    return GetFaceNormal(CubeSectorFace.BACK);
                case CubeSectorFace.TOP:
                    return GetFaceNormal(CubeSectorFace.RIGHT);
                case CubeSectorFace.BOTTOM:
                    return GetFaceNormal(CubeSectorFace.LEFT);
            }
            throw new NotImplementedException();
        }
    }
}
