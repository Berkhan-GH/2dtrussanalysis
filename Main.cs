using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BMSM
{
    public partial class Main : Form
    {
        public Main() // Arayüzün Çağırılması
        {
            InitializeComponent();
        }
        private void Main_Load(object sender, EventArgs e) // Arayüz Açıldıktan Sonra Çağırılacak Olan Fonksiyonlar
        {
            MakeBackgroundGrid();
            TestMaterial();
            UpdateMaterial();
            UpdateLoad();
            CheckBoxes();
        }

        #region Canvas
        private byte ActiveJob = 0;
        private bool showLine = true, showLineID = false, showNode = true, showNodeID = false, showLoad = false, showSupport = true;
        private void CheckBoxes() // Default Olarak İşaretlenen CheckBoxlar
        {
            checkedListBox1.SetItemChecked(0, true);
            checkedListBox1.SetItemChecked(2, true);
            checkedListBox1.SetItemChecked(5, true);
        }
        private void CheckCheckBoxes() // CheckBoxların Denetlenmesi
        {
            if (checkedListBox1.GetItemChecked(0)) { showLine = true; } 
            if (!checkedListBox1.GetItemChecked(0)) { showLine = false; }
            if (checkedListBox1.GetItemChecked(1)) { showLineID = true; }
            if (!checkedListBox1.GetItemChecked(1)) { showLineID = false; }
            if (checkedListBox1.GetItemChecked(2)) { showNode = true; }
            if (!checkedListBox1.GetItemChecked(2)) { showNode = false; }
            if (checkedListBox1.GetItemChecked(3)) { showNodeID = true; }
            if (!checkedListBox1.GetItemChecked(3)) { showNodeID = false; }
            if (checkedListBox1.GetItemChecked(4)) { showLoad = true; }
            if (!checkedListBox1.GetItemChecked(4)) { showLoad = false; }
            if (checkedListBox1.GetItemChecked(5)) { showSupport = true; }
            if (!checkedListBox1.GetItemChecked(5)) { showSupport = false; }
            Canvas.Invalidate();
        }
        private void Canvas_MouseDown(object sender, MouseEventArgs e) // Çizim Panelinde Mouse Down Event
        {
            int x = e.X;
            int y = e.Y;
            SnapToGrid(ref x, ref y);
            Crosshair = new Point(x, y);

            switch (ActiveJob)
            {
                case 0:
                    return;
                case 1:
                    Canvas.Invalidate();
                    ActiveJob = 2;
                    return;
                case 2:
                    return;
                case 3:
                    if (MouseIsOverNode(e.Location, out Node OverNodeIDStart) == true)
                    {
                        Point1 = Crosshair;
                        StartNode = OverNodeIDStart.ID;
                        ActiveJob = 4;
                    }
                    return;
                case 4:
                    if (MouseIsOverNode(e.Location, out Node OverNodeIDEnd) == true && OverNodeIDEnd.ID != StartNode)
                    {
                        int LineID;
                        if (LineList.Count == 0)
                        {
                            LineID = 1;
                        }
                        else { LineID = LineList[LineList.Count - 1].ID + 1; }
                        EndNode = OverNodeIDEnd.ID;
                        int materialID = Convert.ToInt32(dataGridView3.SelectedCells[0].Value);
                        LineList.Add(new Line(LineID, materialID, StartNode, EndNode));
                        Canvas.Invalidate();
                        ActiveJob = 3;
                        UpdateLine();
                    }
                    return;

            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e) // Çizim Panelinde Mouse Move Event
        {
            int x = e.X;
            int y = e.Y;
            SnapToGrid(ref x, ref y);
            Crosshair = new Point(x, y);
            Point MyCoordinate = Appropriate(Crosshair);
            label1.Text = String.Format("({0},{1})", MyCoordinate.X, MyCoordinate.Y);
            switch (ActiveJob)
            {
                case 0:
                    return;
                case 2:
                    Canvas.Invalidate();
                    return;
                case 3:
                    return;
                case 4:
                    Point2 = Crosshair;
                    Canvas.Invalidate();
                    return;
                case 5:
                    return;
            }
        }
        private void Canvas_MouseUp(object sender, MouseEventArgs e) // Çizim Panelinde Mouse Up Event
        {
            int x = e.X;
            int y = e.Y;
            SnapToGrid(ref x, ref y);
            Crosshair = new Point(x, y);

            switch (ActiveJob)
            {
                case 0:
                    return;
                case 2:
                    if (MouseIsOverNode(e.Location, out Node node) == false)
                    {
                        int NodeID;
                        if (NodeList.Count == 0)
                        {
                            NodeID = 1;
                        }
                        else { NodeID = NodeList[NodeList.Count - 1].ID + 1; }
                        NodeList.Add(new Node(NodeID, Crosshair));
                        Canvas.Invalidate();
                        UpdateNode();
                    }
                    ActiveJob = 1;
                    return;
                case 3:
                    return;
                case 4:
                    return;
                case 5:
                    return;
            }
        }
        private void Canvas_Paint(object sender, PaintEventArgs e) // Çizim Panelinin Her Tazelemede Çizeceği Nesneler
        {
            if (showNode)
            {
                foreach (Node node in NodeList)
                {
                    Rectangle pointer1 = new Rectangle(node.NodePoint.X - 2, node.NodePoint.Y - 2, 4, 4);
                    e.Graphics.FillEllipse(Brushes.Red, pointer1);
                    e.Graphics.DrawEllipse(Pens.Black, pointer1);
                    if (showNodeID)
                    {
                        e.Graphics.DrawString("(" + node.ID.ToString() + ")", new Font("Arial", 8), new SolidBrush(Color.Black), new Point(node.NodePoint.X - 16, node.NodePoint.Y - 20));
                    }
                }
            }

            if (showLine)
            {
                foreach (Line line in LineList)
                {
                    Point node1 = NodeList.Find(x => x.ID.Equals(line.StartNode)).NodePoint;
                    Point node2 = NodeList.Find(x => x.ID.Equals(line.EndNode)).NodePoint;
                    e.Graphics.DrawLine(Pens.Blue, node1, node2);
                    if (showLineID)
                    {
                        Point CenterLine = new Point(((node2.X + node1.X) / 2) - 16, ((node2.Y + node1.Y) / 2) - 20);
                        e.Graphics.DrawString("[" + line.ID.ToString() + "]", new Font("Arial", 8), new SolidBrush(Color.Black), CenterLine);
                    }
                }
            }

            if (showLoad)
            {
                foreach (NodalLoad load in LoadList)
                {
                    Point point = NodeList.Find(x => x.ID.Equals(load.Node)).NodePoint;
                    e.Graphics.DrawLine(new Pen(Color.Blue, 1), new Point(point.X, point.Y - 32), new Point(point.X, point.Y));
                    e.Graphics.DrawLine(new Pen(Color.Blue, 1), new Point(point.X - 8, point.Y - 8), new Point(point.X, point.Y));
                    e.Graphics.DrawLine(new Pen(Color.Blue, 1), new Point(point.X + 8, point.Y - 8), new Point(point.X, point.Y));
                }
            }
            if (showSupport)
            {
                foreach(Support support in SupportList)
                {
                    Point point = NodeList.Find(x => x.ID.Equals(support.Node)).NodePoint;
                    switch (support.Type)
                    {
                        case 1:
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X, point.Y), new Point(point.X - 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X, point.Y), new Point(point.X + 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X + 6, point.Y + 6), new Point(point.X - 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X - 6, point.Y + 12), new Point(point.X - 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X + 6, point.Y + 12), new Point(point.X + 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X - 6, point.Y + 12), new Point(point.X + 6, point.Y + 12));
                            break;
                        case 2:
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X, point.Y), new Point(point.X - 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X, point.Y), new Point(point.X + 6, point.Y + 6));
                            e.Graphics.DrawLine(new Pen(Color.Green, 1), new Point(point.X + 6, point.Y + 6), new Point(point.X - 6, point.Y + 6));
                            Rectangle pointer1 = new Rectangle(point.X - 6, point.Y + 6, 5, 5);
                            Rectangle pointer2 = new Rectangle(point.X + 1, point.Y + 6, 5, 5);
                            e.Graphics.DrawEllipse(Pens.Green, pointer1);
                            e.Graphics.DrawEllipse(Pens.Green, pointer2);
                            break;
                    }
                }
            }

            switch (ActiveJob)
            {
                case 0:
                    return;
                case 1:
                    return;
                case 2:
                    Rectangle pointer1 = new Rectangle(Crosshair.X - 4, Crosshair.Y - 4, 8, 8);
                    e.Graphics.FillEllipse(Brushes.Red, pointer1);
                    e.Graphics.DrawEllipse(Pens.Black, pointer1);
                    return;
                case 3:
                    return;
                case 4:
                    e.Graphics.DrawLine(Pens.Black, Point1, Point2);
                    return;
                case 5:
                    return;
            }
        }
        private void checkedListBox1_MouseMove(object sender, MouseEventArgs e) // CheckBox Paneli Üzerinde Mouse Move Event
        {
            CheckCheckBoxes();
        }
        #endregion

        #region Grids
        private void MakeBackgroundGrid() // Arkaplan İçin Grid Oluşturulması
        {
            Bitmap BG = new Bitmap(Canvas.ClientSize.Width, Canvas.ClientSize.Height);
            int GridGap = 20;
            for (int i = 1; i < 64; i++)
            {
                for (int j = 1; j < 36; j++)
                {
                    BG.SetPixel((GridGap * i), (GridGap * j), Color.Black);
                }
            }
            Canvas.BackgroundImage = BG;
            Canvas.Invalidate();
        }
        private void SnapToGrid(ref int x, ref int y) // İmlecin Gridlere Snaplenmesi
        {
            int GridGap = 20;
            x = GridGap * (int)Math.Round((double)x / GridGap);
            y = GridGap * (int)Math.Round((double)y / GridGap);
        }
        #endregion

        #region Line
        List<Line> LineList = new List<Line>();
        Point Crosshair, Point1, Point2;
        int StartNode, EndNode;
        private void drwLine_Click(object sender, EventArgs e) // Line Çizme İşinin Başlatılması
        {
            ActiveJob = 3;
        }
        private void btnLineAdd_Click(object sender, EventArgs e) // Line Ekleme Panelinin Açılması
        {
            panelAddLine.Visible = true;
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();
            comboBox3.Items.Clear();
            foreach (Material material in MaterialList)
            {
                comboBox1.Items.Add(material.Name);
            }
            foreach (Node node in NodeList)
            {
                comboBox2.Items.Add(node.ID);
                comboBox3.Items.Add(node.ID);
            }
        }
        private void btnLinePanelAdd_Click(object sender, EventArgs e) // Line Ekleme Paneli Üzerinden Yeni Kayıt Girişi
        {
            int LineID;
            if (LineList.Count == 0)
            {
                LineID = 1;
            }
            else { LineID = LineList[LineList.Count - 1].ID + 1; }

            LineList.Add(new Line(LineID, MaterialList.Find(x => x.Name.Equals(comboBox1.Text)).ID, Convert.ToInt32(comboBox2.Text), Convert.ToInt32(comboBox3.Text)));
            UpdateLine();
            Canvas.Invalidate();
        }
        private void btnLineRemove_Click(object sender, EventArgs e) // Line Kaldırma
        {
            LineList.Remove(LineList.Find(x => x.ID.Equals(dataGridView2.SelectedCells[0].Value)));
            UpdateLine();
            Canvas.Invalidate();
        }
        private void btnLinePanelX_Click(object sender, EventArgs e) // Line Ekleme Panelini Kapatmak
        {
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();
            comboBox3.Items.Clear();
            panelAddLine.Visible = false;
        }
        private void UpdateLine() // Line Listesini Update Et
        {
            dataGridView2.Rows.Clear();
            foreach (Line line in LineList)
            {
                int id = line.ID;
                int start = line.StartNode;
                int end = line.EndNode;
                string material = MaterialList.Find(x => x.ID.Equals(line.MaterialID)).Name;
                dataGridView2.Rows.Add(id, start, end, material);
            }
        }
        #endregion

        #region Load
        List<NodalLoad> LoadList = new List<NodalLoad>();
        private void btnLoadPanel_Click(object sender, EventArgs e) // Yükleme Ekleme Panelinin Açılması
        {
            panelLoad.Visible = true;
            comboBox4.Items.Clear();
            comboBox5.Items.Clear();

            comboBox5.Items.Add("1");

            foreach (Node node in NodeList)
            {
                comboBox4.Items.Add(node.ID);
            }
        }
        private void btnLoadRemove_Click(object sender, EventArgs e) // Yükleme Kaldırma
        {
            LoadList.Remove(LoadList.Find(x => x.ID.Equals(dataGridView4.SelectedCells[0].Value)));
            UpdateLoad();
            Canvas.Invalidate();
        }
        private void btnLoadPanelAdd_Click(object sender, EventArgs e) // Yükleme Ekleme Paneli Üzerinden Yeni Kayıt Girişi
        {
            int LoadID;
            if (LoadList.Count == 0)
            {
                LoadID = 1;
            }
            else { LoadID = LoadList[LoadList.Count - 1].ID + 1; }
            LoadList.Add(new NodalLoad(LoadID, Convert.ToInt32(comboBox4.Text), Convert.ToInt32(comboBox5.Text), Convert.ToDouble(numericLoadX.Value), Convert.ToDouble(numericLoadY.Value)));
            UpdateLoad();
            Canvas.Invalidate();
        }
        private void btnLoadPanelX_Click(object sender, EventArgs e) // Yükleme Ekleme Panelini Kapatmak
        {
            panelLoad.Visible = false;
        }
        private void UpdateLoad() // Yüklemelerin Update Edilmesi
        {
            dataGridView4.Rows.Clear();
            foreach (NodalLoad load in LoadList)
            {
                dataGridView4.Rows.Add(load.ID, load.Node, load.ClassID, load.xLoad, load.yLoad);
            }
        }
        #endregion

        #region Material
        List<Material> MaterialList = new List<Material>();
        private void TestMaterial()
        {
            MaterialList.Add(new Material(1, "Default", 0.0015, 20000000));
        }
        private void btnMaterialPanelX_Click(object sender, EventArgs e) // Materyal Ekleme Panelini Kapatmak
        {
            textMaterialPanelMaterial.Clear();
            numericMaterialArea.Value = Convert.ToDecimal(0.0015);
            numericMaterialElasticity.Value = Convert.ToDecimal(20000000.0000);
            panelMaterialAdd.Visible = false;
        }
        private void btnMaterialAdd_Click(object sender, EventArgs e) // Materyal Ekleme Panelinin Açılması
        {
            panelMaterialAdd.Visible = true;
        }
        private void btnMaterialAddPanel_Click(object sender, EventArgs e) // Materyal Ekleme Paneli Üzerinden Yeni Kayıt Girişi
        {
            int MaterialID;
            if (MaterialList.Count == 0)
            {
                MaterialID = 1;
            }
            else { MaterialID = MaterialList[MaterialList.Count - 1].ID + 1; }
            MaterialList.Add(new Material(MaterialID, textMaterialPanelMaterial.Text, Convert.ToDouble(numericMaterialArea.Value), Convert.ToDouble(numericMaterialElasticity.Value)));
            UpdateMaterial();
        }
        private void btnMaterialRemove_Click(object sender, EventArgs e) // Materyal Kaldırma
        {
            MaterialList.Remove(MaterialList.Find(x => x.ID.Equals(dataGridView3.SelectedCells[0].Value)));
            UpdateMaterial();
        }
        private void UpdateMaterial() // Malzemelerin Update Edilmesi
        {
            dataGridView3.Rows.Clear();
            foreach (Material material in MaterialList)
            {
                int id = material.ID;
                string name = material.Name;
                double ai = material.Ai;
                double ei = material.Ei;
                dataGridView3.Rows.Add(id, name, ai, ei);
            }
        }
        #endregion

        #region Support
        List<Support> SupportList = new List<Support>();
        private void btnSupportPanel_Click(object sender, EventArgs e) // Support Ekleme Panelinin Açılması
        {
            panelSupport.Visible = true;
            comboBox6.Items.Clear();
            foreach (Node node in NodeList)
            {
                comboBox6.Items.Add(node.ID);
            }
        }
        private void btnSupportPanelAdd_Click(object sender, EventArgs e) // Support Ekleme Paneli Üzerinden Yeni Kayıt Girişi
        {
            int SupportID;
            if (SupportList.Count == 0)
            {
                SupportID = 1;
            }
            else { SupportID = SupportList[SupportList.Count - 1].ID + 1; }
            bool lockX = false, lockY = false;
            if (comboBox7.Text == "Locked")
            {
                lockX = true;
            }
            if (comboBox8.Text == "Locked")
            {
                lockY = true;
            }
            SupportList.Add(new Support(SupportID, Convert.ToInt32(comboBox6.Text), 1, lockX, lockY));
            UpdateSupport();
            Canvas.Invalidate();
        }
        private void UpdateSupport() // Support Listesini Update Et
        {
            dataGridView5.Rows.Clear();
            foreach(Support support in SupportList)
            {
                int id = support.ID;
                int node = support.Node;
                bool lockx = support.xLock;
                bool locky = support.yLock;
                dataGridView5.Rows.Add(id, node, lockx, locky);
            }
        }
        private void btnSupportPanelX_Click(object sender, EventArgs e)
        {
            panelSupport.Visible = false;
        }
        #endregion

        #region Node
        List<Node> NodeList = new List<Node>();
        private void drwNode_Click(object sender, EventArgs e) // Node Çizme İşinin Başlatılması
        {
            ActiveJob = 1;
        }
        private void btnNodeRemove_Click(object sender, EventArgs e) // Node Kaldırma
        {
            NodeList.Remove(NodeList.Find(x => x.ID.Equals(dataGridView1.SelectedCells[0].Value)));
            UpdateNode();
            Canvas.Invalidate();
        }
        private void btnNodeAdd_Click(object sender, EventArgs e) // Node Ekleme Panelinin Açılması
        {
            panelNodeAdd.Visible = true;
        }
        private void btnNodeAddText_Click(object sender, EventArgs e) // Node Ekleme Paneli Üzerinden Yeni Kayıt Girişi
        {
            foreach (string line in textBoxNodeList.Lines)
            {
                if (line != "")
                {
                    int NodeID;
                    if (NodeList.Count == 0)
                    {
                        NodeID = 1;
                    }
                    else { NodeID = NodeList[NodeList.Count - 1].ID + 1; }
                    char splitter = Convert.ToChar(",");
                    string[] str1 = line.Split(splitter);
                    NodeList.Add(new Node(NodeID, new Point(20 + Convert.ToInt32(str1[0]) * 20, Canvas.Height - (20 + Convert.ToInt32(str1[1]) * 20))));
                    Canvas.Invalidate();
                    UpdateNode();
                }
            }
            textBoxNodeList.Clear();
            Canvas.Invalidate();
        }
        private void btnNodePanelX_Click(object sender, EventArgs e) // Node Ekleme Panelinin Kapatmak
        {
            textBoxNodeList.Clear();
            panelNodeAdd.Visible = false;
        }
        private void UpdateNode() // Node Listesini Update Et
        {
            dataGridView1.Rows.Clear();
            foreach (Node node in NodeList)
            {
                int id = node.ID;
                Point point = Appropriate(node.NodePoint);
                dataGridView1.Rows.Add(id, point.X, point.Y);
            }
        }
        private bool MouseIsOverNode(Point mouse_pt, out Node NodeID) // Mouse Node Üzerinde Olduğunda True Veren Boolean
        {
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (FindDistanceToNodeSquared(mouse_pt, NodeList[i].NodePoint) < 16)
                {
                    NodeID = NodeList[i];
                    return true;
                }
            }
            NodeID = null;
            return false;
        }
        private int FindDistanceToNodeSquared(Point Point1, Point Point2) // İki Nokta Arasındaki Mesafenin Karesini Veren Fonksiyon
        {
            int dx = Point1.X - Point2.X;
            int dy = Point1.Y - Point2.Y;
            return dx * dx + dy * dy;
        }
        private Point Appropriate(Point InputPoint) // Uygun Koordinatlara Çevir
        {
            int GridGap = 20;
            int TempX1 = InputPoint.X;
            int TempY1 = InputPoint.Y;
            int TempX2 = (TempX1 / GridGap) - 1;
            int TempY2 = ((Canvas.ClientSize.Height / GridGap) - 1) - ((TempY1 - GridGap) / GridGap);
            Point OutputPoint = new Point(TempX2, TempY2);
            return OutputPoint;
        } 
        #endregion

        #region Export
        private void ExportLines() // Çubukların Export Edilmesi
        {
            DataTable dt = new DataTable(Name = "Line");
            dt.Columns.Add("ID");
            dt.Columns.Add("Material");
            dt.Columns.Add("Start");
            dt.Columns.Add("End");
            dt.Rows.Clear();
            foreach (Line line in LineList)
            {
                dt.Rows.Add(line.ID, line.MaterialID, line.StartNode, line.EndNode);
            }
            DataSet dataSet = new DataSet(Name = "Lines");
            dataSet.Tables.Clear();
            dataSet.Tables.Add(dt);
            dataSet.WriteXml("Lines.xml");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Analysis analysis = new Analysis();
            analysis.Show();
        }

        private void btnSupportRemove_Click(object sender, EventArgs e)
        {
            SupportList.Remove(SupportList.Find(x => x.ID.Equals(dataGridView5.SelectedCells[0].Value)));
            UpdateSupport();
            Canvas.Invalidate();
        }

        private void ExportNodes() // Düğüm Noktalarının Export Edilmesi
        {
            DataTable dt = new DataTable(Name = "Node");
            dt.Columns.Add("ID");
            dt.Columns.Add("x");
            dt.Columns.Add("y");
            dt.Rows.Clear();
            foreach (Node node in NodeList)
            {
                int x = Appropriate(node.NodePoint).X;
                int y = Appropriate(node.NodePoint).Y;
                dt.Rows.Add(node.ID, x, y);
            }
            DataSet dataSet = new DataSet(Name = "Nodes");
            dataSet.Tables.Clear();
            dataSet.Tables.Add(dt);
            dataSet.WriteXml("Nodes.xml");
        }
        private void ExportLoads() // Yüklerin Export Edilmesi
        {
            DataTable dt = new DataTable(Name = "Load");
            dt.Columns.Add("ID");
            dt.Columns.Add("Node ID");
            dt.Columns.Add("Class ID");
            dt.Columns.Add("x");
            dt.Columns.Add("y");
            dt.Rows.Clear();
            foreach (NodalLoad load in LoadList)
            {
                dt.Rows.Add(load.ID, load.Node, load.ClassID, load.xLoad, load.yLoad);
            }
            DataSet dataSet = new DataSet(Name = "Loads");
            dataSet.Tables.Clear();
            dataSet.Tables.Add(dt);
            dataSet.WriteXml("Loads.xml");
        }
        private void ExportMaterials() // Materyallerin Export Edilmesi
        {
            DataTable dt = new DataTable(Name = "Materials");
            dt.Columns.Add("ID");
            dt.Columns.Add("Name");
            dt.Columns.Add("Ai");
            dt.Columns.Add("Ei");
            dt.Rows.Clear();
            foreach (Material material in MaterialList)
            {
                dt.Rows.Add(material.ID, material.Name, material.Ai, material.Ei);
            }
            DataSet dataSet = new DataSet(Name = "Materials");
            dataSet.Tables.Clear();
            dataSet.Tables.Add(dt);
            dataSet.WriteXml("Materials.xml");
        }
        private void ExportSupports() // Desteklerin Export Edilmesi
        {
            DataTable dt = new DataTable(Name = "Support");
            dt.Columns.Add("ID");
            dt.Columns.Add("Node");
            dt.Columns.Add("Type");
            dt.Columns.Add("xLock");
            dt.Columns.Add("yLock");
            dt.Rows.Clear();
            foreach(Support support in SupportList)
            {
                dt.Rows.Add(support.ID, support.Node, support.Type, support.xLock, support.yLock);
            }
            DataSet dataSet = new DataSet(Name = "Supports");
            dataSet.Tables.Clear();
            dataSet.Tables.Add(dt);
            dataSet.WriteXml("Supports.xml");
        }
        private void button3_Click(object sender, EventArgs e) // Export Eventlerin Çağırılması
        {
            ExportLines();
            ExportNodes();
            ExportLoads();
            ExportMaterials();
            ExportSupports();
        }
        private void button4_Click(object sender, EventArgs e) // Çizim Panelinin Görüntüsünün Kaydedilmesi
        {
            using (var bitmap = new Bitmap(Canvas.Width, Canvas.Height))
            {
                Canvas.DrawToBitmap(bitmap, Canvas.ClientRectangle);
                string path = "Canvas.Png";
                bitmap.Save(path, ImageFormat.Png);
            }
        }
        private void button5_Click(object sender, EventArgs e) // Analiz Penceresini Aç
        {
            button3.PerformClick();
            Thread.Sleep(250);
            Calculation calculation = new Calculation();
            calculation.Show();
        }
        #endregion
        private void button1_Click(object sender, EventArgs e) // Debug
        {
            Thread.Sleep(100);
            Calculation calculation = new Calculation();
            calculation.Show();
        }
    }
    public class Node // Düğüm Noktası
    {
        public int ID;
        public Point NodePoint;
        public Node(int ID_, Point NodePoint_)
        {
            ID = ID_;
            NodePoint = NodePoint_;
        }
    }
    public class Line // Çubuk Eleman
    {
        public int ID;
        public int MaterialID;
        public int StartNode;
        public int EndNode;
        public Line(int ID_, int MaterialID_, int StartNode_, int EndNode_)
        {
            ID = ID_;
            MaterialID = MaterialID_;
            StartNode = StartNode_;
            EndNode = EndNode_;
        }
    }
    public class Material // Malzeme Sınıfı
    {
        public int ID;
        public string Name;
        public double Ai;
        public double Ei;
        public Material(int ID_, string Name_, double Ai_, double Ei_)
        {
            ID = ID_;
            Name = Name_;
            Ai = Ai_;
            Ei = Ei_;
        }
    }
    public class NodalLoad // Düğüm Noktasına Yükleme
    {
        public int ID;
        public int Node;
        public int ClassID;
        public double xLoad;
        public double yLoad;
        public NodalLoad(int ID_, int Node_, int ClassID_, double xLoad_, double yLoad_)
        {
            ID = ID_;
            Node = Node_;
            ClassID = ClassID_;
            xLoad = xLoad_;
            yLoad = yLoad_;
        }
    }
    public class Support // Mesnet
    {
        public int ID;
        public int Node;
        public int Type;
        public bool xLock;
        public bool yLock;
        public Support(int ID_, int Node_, int Type_, bool xLock_, bool yLock_)
        {
            ID = ID_;
            Node = Node_;
            Type = Type_;
            xLock = xLock_;
            yLock = yLock_;
        }
    } 
}