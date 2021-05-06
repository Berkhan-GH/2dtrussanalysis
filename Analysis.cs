using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BMSM
{
    public partial class Analysis : Form
    {
        public Analysis()
        {
            InitializeComponent();
        }

        List<Material> MaterialList = new List<Material>();
        List<Support> SupportList = new List<Support>();
        List<NodalLoad> LoadList = new List<NodalLoad>();
        List<xNode> NodeList = new List<xNode>();
        List<Stick> StickList = new List<Stick>();
        List<mKi> KiList = new List<mKi>();
        List<mFi> FiList = new List<mFi>();
        List<int> LockedAxis = new List<int>();
        List<mUCell> Umatrix = new List<mUCell>();
        int NodeCount;
        double[][] mGlobal;
        double[][] mKg;
        double[][] mKgInv;
        double[][] mF;
        double[][] mU;
        private void Analysis_Load(object sender, EventArgs e)
        {
            ImportNodeValues();
            ImportMaterialValues();
            ImportSupportValues();
            Thread.Sleep(500);
            ImportLineValues();
            ImportLoadValues();

            MakeKiMatris();
            MakeGlobalMatris();
            MakeLockedAxis();
            MakeKg();
            MakeKgInv();
            MakeF();
            MakeU();
            MakeFiMatris();

            UpdateData();
            testConsole();
        }

        #region Import
        private void ImportLineValues() // ++
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Lines.xml");
            StickList.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID          = Convert.ToInt32(node.ChildNodes[0].InnerText);
                int material_   = Convert.ToInt32(node.ChildNodes[1].InnerText);

                int node1_ = Convert.ToInt32(node.ChildNodes[2].InnerText);
                double node1x = NodeList.Find(x => x.ID.Equals(node1_)).x;
                double node1y = NodeList.Find(x => x.ID.Equals(node1_)).y;

                int node2_ = Convert.ToInt32(node.ChildNodes[3].InnerText);
                double node2x = NodeList.Find(x => x.ID.Equals(node2_)).x;
                double node2y = NodeList.Find(x => x.ID.Equals(node2_)).y;

                double ai_ = MaterialList.Find(x => x.ID.Equals(material_)).Ai;
                double ei_ = MaterialList.Find(x => x.ID.Equals(material_)).Ei;

                StickList.Add(new Stick(ID, node1_, node1x, node1y, node2_, node2x, node2y, ai_, ei_));
            }
        } 
        private void ImportNodeValues() // ++
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
                NodeList.Add(new xNode(ID, x, y));
            }
        }
        private void ImportMaterialValues() // ++
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
        private void ImportLoadValues() // ++
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Loads.xml");
            LoadList.Clear();
            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                int ID = Convert.ToInt32(node.ChildNodes[0].InnerText);
                int NodeID = Convert.ToInt32(node.ChildNodes[1].InnerText);
                int ClassID = Convert.ToInt32(node.ChildNodes[2].InnerText);
                double xLoad = Convert.ToDouble(node.ChildNodes[3].InnerText);
                double yLoad = Convert.ToDouble(node.ChildNodes[4].InnerText);
                LoadList.Add(new NodalLoad(ID, NodeID, ClassID, xLoad, yLoad));
            }
        }
        private void ImportSupportValues() // ++
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

        private void MakeLockedAxis() // Sınır Koşullarını Denetle
        {
            foreach(Support support in SupportList)
            {
                if (support.xLock)  { LockedAxis.Add((support.Node * 2) - 2); }
                if (support.yLock)  { LockedAxis.Add((support.Node * 2) - 1); }
            }
        } 
        private void MakeKg() // Tersi Alınacak Olan Global Matrisin Oluşturulması
        {
            mKg = MatrixCreate((NodeCount * 2) - LockedAxis.Count, (NodeCount * 2) - LockedAxis.Count);
            int n = 0;
            for (int i = 0; i < mGlobal.Length; i++)
            {
                int m = 0;
                for (int j = 0; j < mGlobal.Length; j++)
                {
                    if (LockedAxis.Contains(i) == false && LockedAxis.Contains(j) == false)
                    {
                        mKg[n][m] = mGlobal[i][j];
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
        private void MakeKgInv() // Tersi Alınacak Olan Global Matrisin Tersinin Alınması
        {
            mKgInv = MatrixCreate(mKg.Length, mKg[0].Length);
            mKgInv = MatrixInverse(mKg);
        }
        private void MakeF() // Yükleme Vektörünün Oluşturulması
        {
            mF = MatrixCreate((NodeList.Count * 2) - LockedAxis.Count,1);
            int n = 0;
            for( int i = 0; i < NodeList.Count; i++)
            {
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 2) == false)
                {
                    if (LoadList.Exists(x => x.Node == NodeList[i].ID))
                    {
                        mF[n][0] = LoadList.Find(x => x.Node.Equals(NodeList[i].ID)).xLoad;
                    }
                    n++;
                }
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 1) == false)
                {
                    if (LoadList.Exists(x => x.Node == NodeList[i].ID))
                    {
                        mF[n][0] = LoadList.Find(x => x.Node.Equals(NodeList[i].ID)).yLoad;
                    }
                    n++;
                }
            }
        }
        private void MakeU() // Deplasman Vektörünün Oluşturulması
        {
            mU = MatrixCreate((NodeList.Count * 2) - LockedAxis.Count, 1);
            mU = MatrixMultiply(mKgInv, mF);

            int n = 0;
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 2) == false)
                {

                    Umatrix.Add(new mUCell(((NodeList[i].ID * 2) - 2), 0));

                    n++;
                }
                if (LockedAxis.Contains((NodeList[i].ID * 2) - 1) == false)
                {
                    Umatrix.Add(new mUCell(((NodeList[i].ID * 2) - 1), 0));
                    n++;
                }
            }
            for(int j = 0; j < mU.Length; j++)
            {
                Umatrix[j].cellV = mU[j][0];
            }
        }
        private void testConsole() // Sonucun Konsol Çıktısı
        {
            Console.Beep();
            foreach (mKi ki in KiList)
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
            Console.Write("Sistem Rijitlik Matrisi"+Environment.NewLine);
            Console.Write(MatrixAsString(mGlobal, 0, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Sistem Rijitlik Matrisi (Tersi Alınacak Olan)" + Environment.NewLine);
            Console.Write(MatrixAsString(mKg, 0, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("Sistem Rijitlik Matrisi Tersi" + Environment.NewLine);
            Console.Write(MatrixAsString(mKgInv, 8, 12));
            Console.Write("*******************************************************************************************************" + Environment.NewLine);
            Console.Write("U Matrisi" + Environment.NewLine);
            //Console.Write(MatrixAsString(mU, 8, 12));

            foreach(mUCell cell in Umatrix)
            {
                Console.Write(cell.axisID.ToString().PadLeft(2) + " "+ cell.cellV.ToString("F8").PadLeft(12) + Environment.NewLine);
            }

            foreach (mFi fi in FiList)
            {
                Console.Write("***************************************************" + Environment.NewLine);
                string s = "";
                Console.Write(fi.ID + " Çubuğu" + Environment.NewLine);
                s += fi.c1.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine; 
                s += fi.c2.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                s += fi.c3.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                s += fi.c4.cellV.ToString("F3").PadLeft(12) + " " + Environment.NewLine;
                Console.Write(s);
            }
        }
        private void MakeKiMatris() // Eleman Rijitlik Matrisi Oluşturulması
        {
            foreach (Stick stck in StickList)
            {
                double _L, _cosa, _sina, _sx, _sy, _ex, _ey, _EAL;
                mCell c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44;

                _L = Math.Sqrt((stck.d5 - stck.d2) * (stck.d5 - stck.d2) + (stck.d6 - stck.d3) * (stck.d6 - stck.d3));
                _cosa = (stck.d5 - stck.d2) / _L; // (node2x - node1x)
                _sina = (stck.d6 - stck.d3) / _L; // (node2y - node1y)
                _EAL = stck.d7 * stck.d8 / _L; // (E*A) / Length

                _sx = stck.d1 * 2 - 1;
                _sy = stck.d1 * 2;
                _ex = stck.d4 * 2 - 1;
                _ey = stck.d4 * 2;

                c11 = new mCell(_sx, _sx, _cosa * _cosa * _EAL);
                c12 = new mCell(_sx, _sy, _cosa * _sina * _EAL);
                c13 = new mCell(_sx, _ex, -_cosa * _cosa * _EAL);
                c14 = new mCell(_sx, _ey, -_cosa * _sina * _EAL);

                c21 = new mCell(_sy, _sx, _cosa * _sina * _EAL);
                c22 = new mCell(_sy, _sy, _sina * _sina * _EAL);
                c23 = new mCell(_sy, _ex, -_cosa * _sina * _EAL);
                c24 = new mCell(_sy, _ey, -_sina * _sina * _EAL);

                c31 = new mCell(_ex, _sx, -_cosa * _cosa * _EAL);
                c32 = new mCell(_ex, _sy, -_cosa * _sina * _EAL);
                c33 = new mCell(_ex, _ex, _cosa * _cosa * _EAL);
                c34 = new mCell(_ex, _ey, _cosa * _sina * _EAL);

                c41 = new mCell(_ey, _sx, -_cosa * _sina * _EAL);
                c42 = new mCell(_ey, _sy, -_sina * _sina * _EAL);
                c43 = new mCell(_ey, _ex, _cosa * _sina * _EAL);
                c44 = new mCell(_ey, _ey, _sina * _sina * _EAL);

                KiList.Add(new mKi(stck.ID, c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44));
            }
        }
        private void MakeFiMatris() // Eleman Uç Kuvvetleri Matrisi Oluşturulması
        {
            foreach(mKi mki in KiList)
            {
                int ID = mki.ID;
                double node1x = ((StickList.Find(x => x.ID.Equals(ID)).d1) * 2) - 2;
                double node1y = ((StickList.Find(x => x.ID.Equals(ID)).d1) * 2) - 1;
                double node2x = ((StickList.Find(x => x.ID.Equals(ID)).d4) * 2) - 2;
                double node2y = ((StickList.Find(x => x.ID.Equals(ID)).d4) * 2) - 1;

                double[][] tempMatrixA = MatrixCreate(4, 4);
                double[][] tempMatrixB = MatrixCreate(4, 1);
                double[][] tempMatrixC = MatrixCreate(4, 1);

                tempMatrixA[0][0] = mki.c11.cellV; tempMatrixA[0][1] = mki.c12.cellV; tempMatrixA[0][2] = mki.c13.cellV; tempMatrixA[0][3] = mki.c14.cellV;
                tempMatrixA[1][0] = mki.c21.cellV; tempMatrixA[1][1] = mki.c22.cellV; tempMatrixA[1][2] = mki.c23.cellV; tempMatrixA[1][3] = mki.c24.cellV;
                tempMatrixA[2][0] = mki.c31.cellV; tempMatrixA[2][1] = mki.c32.cellV; tempMatrixA[2][2] = mki.c33.cellV; tempMatrixA[2][3] = mki.c34.cellV;
                tempMatrixA[3][0] = mki.c41.cellV; tempMatrixA[3][1] = mki.c42.cellV; tempMatrixA[3][2] = mki.c43.cellV; tempMatrixA[3][3] = mki.c44.cellV;

                if (Umatrix.Exists(x => x.axisID == node1x)) { tempMatrixB[0][0] = Umatrix.Find(x => x.axisID.Equals(node1x)).cellV; } else { tempMatrixB[0][0] = 0; }
                if (Umatrix.Exists(x => x.axisID == node1y)) { tempMatrixB[1][0] = Umatrix.Find(x => x.axisID.Equals(node1y)).cellV; } else { tempMatrixB[1][0] = 0; }
                if (Umatrix.Exists(x => x.axisID == node2x)) { tempMatrixB[2][0] = Umatrix.Find(x => x.axisID.Equals(node2x)).cellV; } else { tempMatrixB[2][0] = 0; }
                if (Umatrix.Exists(x => x.axisID == node2y)) { tempMatrixB[3][0] = Umatrix.Find(x => x.axisID.Equals(node2y)).cellV; } else { tempMatrixB[3][0] = 0; }

                tempMatrixC = MatrixMultiply(tempMatrixA, tempMatrixB);

                FiList.Add(new mFi(ID, new mUCell(node1x, tempMatrixC[0][0]), new mUCell(node1y, tempMatrixC[1][0]), new mUCell(node2x, tempMatrixC[2][0]), new mUCell(node2y, tempMatrixC[3][0])));
            }
        }
        private void MakeGlobalMatris() // Global Matrisin Oluşturulması
        {
            List<mCell> TempMatris = new List<mCell>();
            foreach (mKi msk in KiList)
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
            mGlobal = MatrixCreate(NodeCount * 2, NodeCount * 2);
            for (int i = 0; i < mGlobal.Length; i++)
            {
                for (int j = 0; j < mGlobal[0].Length; j++)
                {
                    foreach (mCell mc in TempMatris)
                    {
                        if (mc.lineID == i + 1 && mc.colID == j + 1)
                        {
                            mGlobal[i][j] += mc.cellV;
                        }
                    }
                }
            }
        }

        private void UpdateData()
        {
            comboBox1.Items.Clear();
            foreach(mKi mki in KiList)
            {
                comboBox1.Items.Add(mki.ID);
            }
        }
        private void UpdateData1()
        {
            int i = Convert.ToInt32(comboBox1.Text)-1;
            dataGridView1.Rows.Clear();
            dataGridView1.Rows.Add(KiList[i].c11.cellV, KiList[i].c12.cellV, KiList[i].c13.cellV, KiList[i].c14.cellV);
            dataGridView1.Rows.Add(KiList[i].c21.cellV, KiList[i].c22.cellV, KiList[i].c23.cellV, KiList[i].c24.cellV);
            dataGridView1.Rows.Add(KiList[i].c31.cellV, KiList[i].c32.cellV, KiList[i].c33.cellV, KiList[i].c34.cellV);
            dataGridView1.Rows.Add(KiList[i].c41.cellV, KiList[i].c42.cellV, KiList[i].c43.cellV, KiList[i].c44.cellV);
        }

        #region Matrix Calculations
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateData1();
        }
    }

    class xNode // Düğüm noktası
    {
        public int ID;
        public double x, y;
        public xNode(int ID_, double x_, double y_)
        {
            ID = ID_;
            x = x_;
            y = y_;
        }
    }
    class Stick // Çubuk
    {
        public int ID;
        public double d1, d2, d3, d4, d5, d6, d7, d8;
        public Stick(int ID_, double d1_, double d2_, double d3_, double d4_, double d5_, double d6_, double d7_, double d8_)
        {
            ID = ID_;
            d1 = d1_;
            d2 = d2_;
            d3 = d3_;
            d4 = d4_;
            d5 = d5_;
            d6 = d6_;
            d7 = d7_;
            d8 = d8_;
        }
    }
    class mKi // Her çubuk için K (rijitlik) matrisi (Genel eksen takımına göre)
    {
        public int ID;
        public mCell c11, c12, c13, c14, c21, c22, c23, c24, c31, c32, c33, c34, c41, c42, c43, c44;
        public mKi(int ID_, mCell c11_, mCell c12_, mCell c13_, mCell c14_, mCell c21_, mCell c22_, mCell c23_, mCell c24_, mCell c31_, mCell c32_, mCell c33_, mCell c34_, mCell c41_, mCell c42_, mCell c43_, mCell c44_)
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
    class mFi // Her çubuk için genel eksen takımında kuvvet matrisi
    {
        public int ID;
        public mUCell c1, c2, c3, c4;
        public mFi(int ID_, mUCell c1_, mUCell c2_, mUCell c3_, mUCell c4_)
        {
            ID = ID_;
            c1 = c1_;
            c2 = c2_;
            c3 = c3_;
            c4 = c4_;
        }
    }
    class mCell // Çubukların K (rijitlik) matrislerinin her bir hücresi için oluşturulan object
    {
        public double colID, lineID, cellV;
        public mCell(double lineID_, double colID_, double cellV_)
        {
            colID = colID_;
            lineID = lineID_;
            cellV = cellV_;
        }
    }
    class mUCell // Deplasman matrisinin her bir hücresi için oluşturulan object
    {
        public double axisID,cellV;
        public mUCell(double axisID_, double cellV_)
        {
            axisID = axisID_;
            cellV = cellV_;
        }
    }
}