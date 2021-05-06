using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BMSM
{
    public partial class Calculation : Form
    {
        public Calculation()
        {
            InitializeComponent();
        }
        //////////////////////////////////////////////////
        #region VARIABLES
        List<Node_> NodeList = new List<Node_>();
        List<Stick_> StickList = new List<Stick_>();
        List<Support> SupportList = new List<Support>();
        List<NodalLoad> LoadList = new List<NodalLoad>();
        List<Material> MaterialList = new List<Material>();

        List<Fi> FiList = new List<Fi>();
        List<Fi> FiLList = new List<Fi>();
        List<Kl> KlList = new List<Kl>();
        List<Kl> KgList = new List<Kl>();
        List<Kl> TList = new List<Kl>();
        List<UCell> UMatrix = new List<UCell>();
        List<int> LockedAxis = new List<int>(); 
        List<SupportR> SupportsR = new List<SupportR>();
        private int NodeCount = 0;

        private double[][] FMatrix;
        private double[][] GlobalMatrix;
        private double[][] GlobalMatrix_;
        private double[][] GlobalMatrix_I;
        private double[][] UMatrix_;



        #endregion
        //////////////////////////////////////////////////
        private void Calculation_Load(object sender, EventArgs e)
        {
            TextBoxWriter writer = new TextBoxWriter(textBox1);
            Console.SetOut(writer);
            ImportNode();
            ImportMaterial();
            ImportSupport();
            Thread.Sleep(100);
            ImportLoad();
            ImportLine();
            Thread.Sleep(100);
            MakeLockedAxis();
            MakeKgMatrix();
            MakeKlMatrix();
            MakeTMatrix();
            Thread.Sleep(100);
            MakeFMatrix();
            MakeGlobalMatrix();
            Thread.Sleep(100);
            MakeGlobalMatrix_();
            Thread.Sleep(100);
            MakeGlobalMatrix_I();
            Thread.Sleep(100);
            MakeUMatrix();
            Thread.Sleep(100);
            MakeFiMatrix();
            Thread.Sleep(100);
            MakeFiLMatrix();
            Thread.Sleep(100);
            MakeSupportReaction();
            Thread.Sleep(100);
            ConsoleWrite();
        }
        //////////////////////////////////////////////////
        #region IMPORT
        private void ImportLine()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Lines.xml");
            StickList.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                Material material = MaterialList.Find(x => x.ID.Equals(Convert.ToInt32(node.ChildNodes[1].InnerText)));
                Node_ startnode = NodeList.Find(x => x.ID.Equals(Convert.ToInt32(node.ChildNodes[2].InnerText)));
                Node_ endnode = NodeList.Find(x => x.ID.Equals(Convert.ToInt32(node.ChildNodes[3].InnerText)));
                StickList.Add(new Stick_(ID, material, startnode, endnode));
            }
        }
        private void ImportLoad()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Loads.xml");
            LoadList.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                int NodeID = Convert.ToInt32(node.ChildNodes[1].InnerText);
                int ClassID = Convert.ToInt32(node.ChildNodes[2].InnerText);
                double xLoad = Convert.ToDouble(node.ChildNodes[3].InnerText);
                double yLoad = Convert.ToDouble(node.ChildNodes[4].InnerText);
                LoadList.Add(new NodalLoad(ID, NodeID, ClassID, xLoad, yLoad));
            }
        }
        private void ImportMaterial()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Materials.xml");
            MaterialList.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                string Name = node.ChildNodes[1].InnerText;
                double Ai = Convert.ToDouble(node.ChildNodes[2].InnerText);
                double Ei = Convert.ToDouble(node.ChildNodes[3].InnerText);
                MaterialList.Add(new Material(ID, Name, Ai, Ei));
            }
        }
        private void ImportNode()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Nodes.xml");
            NodeList.Clear();
            NodeCount = doc.DocumentElement.ChildNodes.Count;
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                int x = Convert.ToInt32(node.ChildNodes[1].InnerText);
                int y = Convert.ToInt32(node.ChildNodes[2].InnerText);
                NodeList.Add(new Node_(ID, x, y));
            }
        }
        private void ImportSupport()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Supports.xml");
            SupportList.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                int NodeID = Convert.ToInt32(node.ChildNodes[1].InnerText);
                int TypeID = Convert.ToInt32(node.ChildNodes[2].InnerText);
                bool xLock = Convert.ToBoolean(node.ChildNodes[3].InnerText);
                bool yLock = Convert.ToBoolean(node.ChildNodes[4].InnerText);
                SupportList.Add(new Support(ID, NodeID, TypeID, xLock, yLock));
            }
        }
        #endregion
        //////////////////////////////////////////////////
        #region MAKE
        private void MakeFMatrix()
        {
            FMatrix = MatrixCreate((NodeList.Count * 2) - LockedAxis.Count, 1);
            int n = 0;
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 2) == false)
                {
                    if (LoadList.Exists(x => x.Node == NodeList[i].ID))
                    {
                        FMatrix[n][0] = LoadList.Find(x => x.Node.Equals(NodeList[i].ID)).xLoad;
                    }
                    n++;
                }
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 1) == false)
                {
                    if (LoadList.Exists(x => x.Node == NodeList[i].ID))
                    {
                        FMatrix[n][0] = LoadList.Find(x => x.Node.Equals(NodeList[i].ID)).yLoad;
                    }
                    n++;
                }
            }
        }
        private void MakeFiMatrix()
        {
            foreach (Kl mki in KgList)
            {
                int ID = mki.ID;
                double node1x = ((StickList.Find(x => x.ID.Equals(ID)).StartS.ID) * 2) - 2;
                double node1y = ((StickList.Find(x => x.ID.Equals(ID)).StartS.ID) * 2) - 1;
                double node2x = ((StickList.Find(x => x.ID.Equals(ID)).EndS.ID) * 2) - 2;
                double node2y = ((StickList.Find(x => x.ID.Equals(ID)).EndS.ID) * 2) - 1;

                double[][] tempMatrixA = MatrixCreate(4, 4);
                double[][] tempMatrixB = MatrixCreate(4, 1);
                double[][] tempMatrixC = MatrixCreate(4, 1);

                tempMatrixA[0][0] = mki.c11.cellV; tempMatrixA[0][1] = mki.c12.cellV; tempMatrixA[0][2] = mki.c13.cellV; tempMatrixA[0][3] = mki.c14.cellV;
                tempMatrixA[1][0] = mki.c21.cellV; tempMatrixA[1][1] = mki.c22.cellV; tempMatrixA[1][2] = mki.c23.cellV; tempMatrixA[1][3] = mki.c24.cellV;
                tempMatrixA[2][0] = mki.c31.cellV; tempMatrixA[2][1] = mki.c32.cellV; tempMatrixA[2][2] = mki.c33.cellV; tempMatrixA[2][3] = mki.c34.cellV;
                tempMatrixA[3][0] = mki.c41.cellV; tempMatrixA[3][1] = mki.c42.cellV; tempMatrixA[3][2] = mki.c43.cellV; tempMatrixA[3][3] = mki.c44.cellV;

                if (UMatrix.Exists(x => x.axisID == node1x)) { tempMatrixB[0][0] = UMatrix.Find(x => x.axisID.Equals(node1x)).cellV; } else { tempMatrixB[0][0] = 0; }
                if (UMatrix.Exists(x => x.axisID == node1y)) { tempMatrixB[1][0] = UMatrix.Find(x => x.axisID.Equals(node1y)).cellV; } else { tempMatrixB[1][0] = 0; }
                if (UMatrix.Exists(x => x.axisID == node2x)) { tempMatrixB[2][0] = UMatrix.Find(x => x.axisID.Equals(node2x)).cellV; } else { tempMatrixB[2][0] = 0; }
                if (UMatrix.Exists(x => x.axisID == node2y)) { tempMatrixB[3][0] = UMatrix.Find(x => x.axisID.Equals(node2y)).cellV; } else { tempMatrixB[3][0] = 0; }

                tempMatrixC = MatrixMultiply(tempMatrixA, tempMatrixB);

                FiList.Add(new Fi(ID, new UCell(node1x, tempMatrixC[0][0]), new UCell(node1y, tempMatrixC[1][0]), new UCell(node2x, tempMatrixC[2][0]), new UCell(node2y, tempMatrixC[3][0])));
            }
        }
        private void MakeFiLMatrix()
        {
            foreach (Fi fi in FiList)
            {
                int ID = fi.ID;
                double node1x = ((StickList.Find(x => x.ID.Equals(ID)).StartS.ID) * 2) - 2;
                double node1y = ((StickList.Find(x => x.ID.Equals(ID)).StartS.ID) * 2) - 1;
                double node2x = ((StickList.Find(x => x.ID.Equals(ID)).EndS.ID) * 2) - 2;
                double node2y = ((StickList.Find(x => x.ID.Equals(ID)).EndS.ID) * 2) - 1;
                Kl mki = TList.Find(x => x.ID.Equals(ID));
                double[][] tempMatrixA = MatrixCreate(4, 4);
                double[][] tempMatrixB = MatrixCreate(4, 1);
                double[][] tempMatrixC = MatrixCreate(4, 1);

                tempMatrixA[0][0] = mki.c11.cellV; tempMatrixA[0][1] = mki.c12.cellV; tempMatrixA[0][2] = mki.c13.cellV; tempMatrixA[0][3] = mki.c14.cellV;
                tempMatrixA[1][0] = mki.c21.cellV; tempMatrixA[1][1] = mki.c22.cellV; tempMatrixA[1][2] = mki.c23.cellV; tempMatrixA[1][3] = mki.c24.cellV;
                tempMatrixA[2][0] = mki.c31.cellV; tempMatrixA[2][1] = mki.c32.cellV; tempMatrixA[2][2] = mki.c33.cellV; tempMatrixA[2][3] = mki.c34.cellV;
                tempMatrixA[3][0] = mki.c41.cellV; tempMatrixA[3][1] = mki.c42.cellV; tempMatrixA[3][2] = mki.c43.cellV; tempMatrixA[3][3] = mki.c44.cellV;

                tempMatrixB[0][0] = fi.c1.cellV;
                tempMatrixB[1][0] = fi.c2.cellV;
                tempMatrixB[2][0] = fi.c3.cellV;
                tempMatrixB[3][0] = fi.c4.cellV;

                tempMatrixC = MatrixMultiply(tempMatrixA, tempMatrixB);

                FiLList.Add(new Fi(ID, new UCell(node1x, tempMatrixC[0][0]), new UCell(node1y, tempMatrixC[1][0]), new UCell(node2x, tempMatrixC[2][0]), new UCell(node2y, tempMatrixC[3][0])));
            }
        }
        private void MakeGlobalMatrix()
        {
            List<KlCell> TempMatris = new List<KlCell>();
            foreach (Kl msk in KgList)
            {
                TempMatris.Add(msk.c11);
                TempMatris.Add(msk.c12);
                TempMatris.Add(msk.c13);
                TempMatris.Add(msk.c14);
                TempMatris.Add(msk.c21);
                TempMatris.Add(msk.c22);
                TempMatris.Add(msk.c23);
                TempMatris.Add(msk.c24);
                TempMatris.Add(msk.c31);
                TempMatris.Add(msk.c32);
                TempMatris.Add(msk.c33);
                TempMatris.Add(msk.c34);
                TempMatris.Add(msk.c41);
                TempMatris.Add(msk.c42);
                TempMatris.Add(msk.c43);
                TempMatris.Add(msk.c44);
            }
            GlobalMatrix = MatrixCreate(NodeCount * 2, NodeCount * 2);
            for (int i = 0; i < GlobalMatrix.Length; i++)
            {
                for (int j = 0; j < GlobalMatrix[0].Length; j++)
                {
                    foreach (KlCell mc in TempMatris)
                    {
                        if (mc.lineID == i + 1 && mc.colID == j + 1)
                        {
                            GlobalMatrix[i][j] += mc.cellV;
                        }
                    }
                }
            }
        }
        private void MakeGlobalMatrix_()
        {
            GlobalMatrix_ = MatrixCreate((NodeCount * 2) - LockedAxis.Count, (NodeCount * 2) - LockedAxis.Count);
            int n = 0;
            for (int i = 0; i < GlobalMatrix.Length; i++)
            {
                int m = 0;
                for (int j = 0; j < GlobalMatrix.Length; j++)
                {
                    if (LockedAxis.Contains(i) == false && LockedAxis.Contains(j) == false)
                    {
                        GlobalMatrix_[n][m] = GlobalMatrix[i][j];
                    }
                    if (LockedAxis.Contains(j) == false)
                    {
                        m++;
                    }
                }
                if (LockedAxis.Contains(i) == false)
                {
                    n++;
                }
            }
        }
        private void MakeGlobalMatrix_I()
        {
            GlobalMatrix_I = MatrixCreate(GlobalMatrix_.Length, GlobalMatrix_[0].Length);
            GlobalMatrix_I = MatrixInverse(GlobalMatrix_);
        }
        private void MakeKgMatrix()
        {
            foreach (Stick_ stick in StickList)
            {
                double E_, A_, L_, Cos_, Sin_, EAL_, SxID, SyID, ExID, EyID;
                KlCell c11, c12, c13, c14,
                       c21, c22, c23, c24,
                       c31, c32, c33, c34,
                       c41, c42, c43, c44;

                E_ = stick.MaterialS.Ei;
                A_ = stick.MaterialS.Ai;
                L_ = Math.Sqrt((stick.EndS.x - stick.StartS.x) * (stick.EndS.x - stick.StartS.x)
                            + (stick.EndS.y - stick.StartS.y) * (stick.EndS.y - stick.StartS.y));
                EAL_ = E_ * A_ / L_;
                Cos_ = (stick.EndS.x - stick.StartS.x) / L_;
                Sin_ = (stick.EndS.y - stick.StartS.y) / L_;
                SxID = stick.StartS.ID * 2 - 1;
                SyID = stick.StartS.ID * 2;
                ExID = stick.EndS.ID * 2 - 1;
                EyID = stick.EndS.ID * 2;

                c11 = new KlCell(SxID, SxID, Cos_* Cos_ * EAL_);
                c12 = new KlCell(SxID, SyID, Cos_ * Sin_ * EAL_);
                c13 = new KlCell(SxID, ExID, -Cos_ * Cos_ * EAL_);
                c14 = new KlCell(SxID, EyID, -Cos_ * Sin_ * EAL_);

                c21 = new KlCell(SyID, SxID, Cos_ * Sin_ * EAL_);
                c22 = new KlCell(SyID, SyID, Sin_ * Sin_ * EAL_);
                c23 = new KlCell(SyID, ExID, -Cos_ * Sin_ * EAL_);
                c24 = new KlCell(SyID, EyID, -Sin_ * Sin_ * EAL_);

                c31 = new KlCell(ExID, SxID, -Cos_ * Cos_ * EAL_);
                c32 = new KlCell(ExID, SyID, -Cos_ * Sin_ * EAL_);
                c33 = new KlCell(ExID, ExID, Cos_ * Cos_ * EAL_);
                c34 = new KlCell(ExID, EyID, Cos_ * Sin_ * EAL_);

                c41 = new KlCell(EyID, SxID, -Cos_ * Sin_ * EAL_);
                c42 = new KlCell(EyID, SyID, -Sin_ * Sin_ * EAL_);
                c43 = new KlCell(EyID, ExID, Cos_ * Sin_ * EAL_);
                c44 = new KlCell(EyID, EyID, Sin_ * Sin_ * EAL_);

                KgList.Add(new Kl(stick.ID, c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44));
            }
        }
        private void MakeKlMatrix()
        {
            foreach (Stick_ stick in StickList)
            {
                double E_, A_, L_, EAL_, SxID, SyID, ExID, EyID;
                KlCell c11, c12, c13, c14,
                       c21, c22, c23, c24,
                       c31, c32, c33, c34,
                       c41, c42, c43, c44;

                E_ = stick.MaterialS.Ei;
                A_ = stick.MaterialS.Ai;
                L_ = Math.Sqrt((stick.EndS.x - stick.StartS.x) * (stick.EndS.x - stick.StartS.x)
                            + (stick.EndS.y - stick.StartS.y) * (stick.EndS.y - stick.StartS.y));
                EAL_ = E_ * A_ / L_;
                SxID = stick.StartS.ID * 2 - 1;
                SyID = stick.StartS.ID * 2;
                ExID = stick.EndS.ID * 2 - 1;
                EyID = stick.EndS.ID * 2;

                c11 = new KlCell(SxID, SxID, 1 * EAL_);
                c12 = new KlCell(SxID, SyID, 0 * EAL_);
                c13 = new KlCell(SxID, ExID, -1 * EAL_);
                c14 = new KlCell(SxID, EyID, 0 * EAL_);

                c21 = new KlCell(SyID, SxID, 0 * EAL_);
                c22 = new KlCell(SyID, SyID, 0 * EAL_);
                c23 = new KlCell(SyID, ExID, 0 * EAL_);
                c24 = new KlCell(SyID, EyID, 0 * EAL_);

                c31 = new KlCell(ExID, SxID, -1 * EAL_);
                c32 = new KlCell(ExID, SyID, 0 * EAL_);
                c33 = new KlCell(ExID, ExID, 1 * EAL_);
                c34 = new KlCell(ExID, EyID, 0 * EAL_);

                c41 = new KlCell(EyID, SxID, 0 * EAL_);
                c42 = new KlCell(EyID, SyID, 0 * EAL_);
                c43 = new KlCell(EyID, ExID, 0 * EAL_);
                c44 = new KlCell(EyID, EyID, 0 * EAL_);

                KlList.Add(new Kl(stick.ID, c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44));
            }
        }
        private void MakeLockedAxis()
        {
            foreach (Support support in SupportList)
            {
                if (support.xLock) { LockedAxis.Add((support.Node * 2) - 2); }
                if (support.yLock) { LockedAxis.Add((support.Node * 2) - 1); }
            }
        }
        private void MakeSupportReaction()
        {
            foreach(Support support in SupportList)
            {
                int nodeID = support.Node;
                double nodeRx = 0;
                double nodeRy = 0;
                foreach (Stick_ stick in StickList)
                {
                    if (stick.StartS.ID == nodeID)
                    {
                        nodeRx += FiList.Find(x => x.ID.Equals(stick.ID)).c1.cellV;
                        nodeRy += FiList.Find(x => x.ID.Equals(stick.ID)).c2.cellV;
                    }
                    if (stick.EndS.ID == nodeID)
                    {
                        nodeRx += FiList.Find(x => x.ID.Equals(stick.ID)).c3.cellV;
                        nodeRy += FiList.Find(x => x.ID.Equals(stick.ID)).c4.cellV;
                    }
                }
                SupportsR.Add(new SupportR(nodeID, nodeRx, nodeRy));
            }
        }
        private void MakeTMatrix()
        {
            foreach(Stick_ stick in StickList)
            {
                double Cos_, Sin_, L_, SxID, SyID, ExID, EyID;
                KlCell c11, c12, c13, c14, 
                       c21, c22, c23, c24, 
                       c31, c32, c33, c34, 
                       c41, c42, c43, c44;

                L_= Math.Sqrt((stick.EndS.x - stick.StartS.x) * (stick.EndS.x - stick.StartS.x) 
                            + (stick.EndS.y - stick.StartS.y) * (stick.EndS.y - stick.StartS.y));
                Cos_ = (stick.EndS.x - stick.StartS.x) / L_;
                Sin_ = (stick.EndS.y - stick.StartS.y) / L_;
                SxID = stick.StartS.ID * 2 - 1;
                SyID = stick.StartS.ID * 2;
                ExID = stick.EndS.ID * 2 - 1;
                EyID = stick.EndS.ID * 2;

                c11 = new KlCell(SxID, SxID, Cos_);
                c12 = new KlCell(SxID, SyID, Sin_);
                c13 = new KlCell(SxID, ExID, 0);
                c14 = new KlCell(SxID, EyID, 0);

                c21 = new KlCell(SyID, SxID, -Sin_);
                c22 = new KlCell(SyID, SyID, Cos_);
                c23 = new KlCell(SyID, ExID, 0);
                c24 = new KlCell(SyID, EyID, 0);

                c31 = new KlCell(ExID, SxID, 0);
                c32 = new KlCell(ExID, SyID, 0);
                c33 = new KlCell(ExID, ExID, Cos_);
                c34 = new KlCell(ExID, EyID, Sin_);

                c41 = new KlCell(EyID, SxID, 0);
                c42 = new KlCell(EyID, SyID, 0);
                c43 = new KlCell(EyID, ExID, -Sin_);
                c44 = new KlCell(EyID, EyID, Cos_);

                TList.Add(new Kl(stick.ID, c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44));
            }
        }
        private void MakeUMatrix()
        {
            UMatrix_ = MatrixCreate((NodeList.Count * 2) - LockedAxis.Count, 1);
            UMatrix_ = MatrixMultiply(GlobalMatrix_I,FMatrix);

            int n = 0;
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 2) == false)
                {

                    UMatrix.Add(new UCell(((NodeList[i].ID * 2) - 2), 0));

                    n++;
                }
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 1) == false)
                {
                    UMatrix.Add(new UCell(((NodeList[i].ID * 2) - 1), 0));
                    n++;
                }
            }
            for (int j = 0; j < UMatrix_.Length; j++)
            {
                UMatrix[j].cellV = UMatrix_[j][0];
            }
        }
        #endregion
        //////////////////////////////////////////////////
        #region MATRIX CALCULATIONS
        private double[][] MatrixCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }
        private double[][] MatrixMultiply(double[][] matrixA, double[][] matrixB)
        {
            double[][] result = MatrixCreate(matrixA.Length, matrixB[0].Length);
            for (int i = 0; i < matrixA[0].Length; i++)
            {
                for (int j = 0; j < matrixB[0].Length; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matrixA.Length; k++)
                    {
                        sum += matrixA[i][k] * matrixB[k][j];
                    }
                    result[i][j] = sum;
                }
            }
            return result;
        }
        private double[][] MatrixInverse(double[][] matrix)
        {
            int n = matrix.Length;
            double[][] result = MatrixDuplicate(matrix);
            int[] perm;
            int toggle;
            double[][] lum = MatrixDecompose(matrix, out perm, out toggle);
            if (lum == null)
                throw new Exception("Unable to compute inverse");
            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }
                double[] x = HelperSolve(lum, b);
                for (int j = 0; j < n; ++j)
                    result[j][i] = x[j];
            }
            return result;
        }
        private double[][] MatrixTranspose(double[][] matrix)
        {
            double[][] result = MatrixCreate(matrix[0].Length, matrix.Length);
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[0].Length; j++)
                {
                    result[j][i] = matrix[i][j];
                }
            }
            return result;
        }
        private double[][] MatrixDuplicate(double[][] matrix)
        {
            double[][] result = MatrixCreate(matrix.Length, matrix[0].Length);
            for (int i = 0; i < matrix.Length; ++i)
                for (int j = 0; j < matrix[i].Length; ++j)
                    result[i][j] = matrix[i][j];
            return result;
        }
        private double[][] MatrixDecompose(double[][] matrix, out int[] perm, out int toggle)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            if (rows != cols)
                throw new Exception("Kare matris değil!");

            int n = rows;

            double[][] result = MatrixDuplicate(matrix);

            perm = new int[n];
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1;

            for (int j = 0; j < n - 1; ++j)
            {
                double colMax = Math.Abs(result[j][j]);
                int pRow = j;

                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i][j]) > colMax)
                    {
                        colMax = Math.Abs(result[i][j]);
                        pRow = i;
                    }
                }

                if (pRow != j)
                {
                    double[] rowPtr = result[pRow];
                    result[pRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[pRow];
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle;
                }
                if (result[j][j] == 0.0)
                {
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row][j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        throw new Exception("Doolittle Metodu kullanılamıyor.");

                    double[] rowPtr = result[goodRow];
                    result[goodRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[goodRow];
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle;
                }

                for (int i = j + 1; i < n; ++i)
                {
                    result[i][j] /= result[j][j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i][k] -= result[i][j] * result[j][k];
                    }
                }


            }
            return result;
        }
        private double[] HelperSolve(double[][] luMatrix, double[] b)
        {
            int n = luMatrix.Length;
            double[] x = new double[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1][n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum / luMatrix[i][i];
            }

            return x;
        }
        private string MatrixAsString(double[][] matrix, int Decimals_, int Padding_)
        {
            string s = "";
            for (int i = 0; i < matrix.Length; ++i)
            {
                for (int j = 0; j < matrix[i].Length; ++j)
                    s += matrix[i][j].ToString("F" + Decimals_.ToString()).PadLeft(Padding_) + " ";
                s += Environment.NewLine;
            }
            return s;
        }
        #endregion
        //////////////////////////////////////////////////
        private void ConsoleWrite()
        {
            Console.Beep();
            foreach (Kl ki in KgList)
            {
                Console.Write("***************************************************" + Environment.NewLine);
                string s = "";
                Console.Write(ki.ID + " Çubuğu" + Environment.NewLine);
                s += ki.c11.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c12.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c13.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c14.cellV.ToString("F0").PadLeft(12) + " ";
                s += Environment.NewLine;

                s += ki.c21.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c22.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c23.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c24.cellV.ToString("F0").PadLeft(12) + " ";
                s += Environment.NewLine;

                s += ki.c31.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c32.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c33.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c34.cellV.ToString("F0").PadLeft(12) + " ";
                s += Environment.NewLine;

                s += ki.c41.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c42.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c43.cellV.ToString("F0").PadLeft(12) + " ";
                s += ki.c44.cellV.ToString("F0").PadLeft(12) + " ";
                s += Environment.NewLine;

                Console.Write(s);
            }
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Sistem Rijitlik Matrisi" + Environment.NewLine);
            Console.Write(MatrixAsString(GlobalMatrix, 0, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Sistem Rijitlik Matrisi (Tersi Alınacak Olan)" + Environment.NewLine);
            Console.Write(MatrixAsString(GlobalMatrix_, 0, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Sistem Rijitlik Matrisi Tersi" + Environment.NewLine);
            Console.Write(MatrixAsString(GlobalMatrix_I, 8, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Deplasmanlar" + Environment.NewLine);

            foreach (UCell cell in UMatrix)
            {
                string axis_;
                int cellID = (int)cell.axisID+1;
                if (cellID % 2 == 0) { axis_ = string.Format("{0}y", cellID / 2); } else { axis_ = string.Format("{0}x", (cellID+1) / 2); }
                Console.Write(axis_ + " " + cell.cellV.ToString("F8").PadLeft(12) + Environment.NewLine);
            }

            foreach (Fi fi in FiLList)
            {
                Console.Write("***************************************************" + Environment.NewLine);
                string s = "";
                Console.Write(fi.ID + " Çubuğu" + Environment.NewLine);
                s += fi.c1.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                s += fi.c2.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                s += fi.c3.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                s += fi.c4.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                Console.Write(s);
                double A_ = StickList.Find(x => x.ID.Equals(fi.ID)).MaterialS.Ai;
                string str = "Gerilme: "+(fi.c3.cellV/A_).ToString("F3") + Environment.NewLine;
                Console.Write(str);
            }
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Reaksiyonlar" + Environment.NewLine);
            foreach(SupportR support in SupportsR)
            {
                string str = string.Format("{0}. Mesnet", support.NodeID).PadLeft(10) + string.Format("Rx: {0}", support.Rx.ToString("F3")).PadLeft(20) + string.Format("Ry: {0}", support.Ry.ToString("F3")).PadLeft(30) + Environment.NewLine;
                Console.Write(str);
            }
        }
    }
    public class TextBoxWriter : TextWriter
    {
        // The control where we will write text.
        private Control MyControl;
        public TextBoxWriter(Control control)
        {
            MyControl = control;
        }

        public override void Write(char value)
        {
            MyControl.Text += value;
        }

        public override void Write(string value)
        {
            MyControl.Text += value;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }

    public class Fi
    {
        public int ID;
        public UCell c1, c2, c3, c4;
        public Fi(int ID_, UCell c1_, UCell c2_, UCell c3_, UCell c4_)
        {
            ID = ID_;
            c1 = c1_;
            c2 = c2_;
            c3 = c3_;
            c4 = c4_;
        }
    }
    public class Node_
    {
        public int ID;
        public double x, y;
        public Node_(int ID_, double x_, double y_)
        {
            ID = ID_;
            x = x_;
            y = y_;
        }
    }
    public class Kl
    {
        public int ID;
        public KlCell c11, c12, c13, c14, 
                      c21, c22, c23, c24, 
                      c31, c32, c33, c34, 
                      c41, c42, c43, c44;
        public Kl(int ID_, KlCell c11_, KlCell c12_, KlCell c13_, KlCell c14_,
                           KlCell c21_, KlCell c22_, KlCell c23_, KlCell c24_,
                           KlCell c31_, KlCell c32_, KlCell c33_, KlCell c34_,
                           KlCell c41_, KlCell c42_, KlCell c43_, KlCell c44_)
        {
            ID = ID_;
            c11 = c11_;
            c12 = c12_;
            c13 = c13_;
            c14 = c14_;
            c21 = c21_;
            c22 = c22_;
            c23 = c23_;
            c24 = c24_;
            c31 = c31_;
            c32 = c32_;
            c33 = c33_;
            c34 = c34_;
            c41 = c41_;
            c42 = c42_;
            c43 = c43_;
            c44 = c44_;
        }
    }
    public class KlCell
    {
        public double colID, lineID, cellV;
        public KlCell(double lineID_, double colID_, double cellV_)
        {
            colID = colID_;
            lineID = lineID_;
            cellV = cellV_;
        }
    }
    public class Stick_
    {
        public int ID;
        public Material MaterialS;
        public Node_ StartS, EndS;
        public Stick_(int ID_, Material MaterialS_, Node_ StartS_, Node_ EndS_)
        {
            ID = ID_;
            MaterialS = MaterialS_;
            StartS = StartS_;
            EndS = EndS_;
        }
    }
    public class SupportR
    {
        public int NodeID;
        public double Rx;
        public double Ry;
        public SupportR(int NodeID_, double Rx_, double Ry_)
        {
            NodeID = NodeID_;
            Rx = Rx_;
            Ry = Ry_;
        }
    }
    public class UCell
    {
        public double axisID, cellV;
        public UCell(double axisID_, double cellV_)
        {
            axisID = axisID_;
            cellV = cellV_;
        }
    }

}