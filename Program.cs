using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NearestVehicleFinder
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public string VehicleRegistration { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public ulong RecordedTimeUTC { get; set; }
    }

    public class QuadTreeNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<Vehicle> Points { get; set; }
        public QuadTreeNode[] Children { get; set; }

        public QuadTreeNode(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Points = new List<Vehicle>();
            Children = new QuadTreeNode[4];
        }

        public bool Insert(Vehicle vehicle)
        {
            if (!_Contains(vehicle))
                return false;

            if (Points.Count < 4)
            {
                Points.Add(vehicle);
                return true;
            }

            if (Children.All(child => child == null))
                _Split();

            foreach (var child in Children)
            {
                if (child.Insert(vehicle))
                    return true;
            }

            return false;
        }

        public bool _Contains(Vehicle vehicle)
        {
            return X <= vehicle.Latitude && vehicle.Latitude < X + Width &&
                   Y <= vehicle.Longitude && vehicle.Longitude < Y + Height;
        }

        private void _Split()
        {
            var halfWidth = Width / 2;
            var halfHeight = Height / 2;

            Children[0] = new QuadTreeNode(X, Y, halfWidth, halfHeight);
            Children[1] = new QuadTreeNode(X + halfWidth, Y, halfWidth, halfHeight);
            Children[2] = new QuadTreeNode(X, Y + halfHeight, halfWidth, halfHeight);
            Children[3] = new QuadTreeNode(X + halfWidth, Y + halfHeight, halfWidth, halfHeight);

            foreach (var point in Points)
            {
                foreach (var child in Children)
                {
                    if (child.Insert(point))
                        break;
                }
            }

            Points.Clear();
        }
    }

    public class NearestVehicleFinder
    {
        private static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private static Vehicle FindNearestVehicle(QuadTreeNode quadtree, double x, double y)
        {
            Vehicle nearestVehicle = null;
            double bestDistance = double.PositiveInfinity;

            void FindNearestRec(QuadTreeNode node)
            {
                foreach (var point in node.Points)
                {
                    var distance = CalculateDistance(x, y, point.Latitude, point.Longitude);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearestVehicle = point;
                    }
                }

                foreach (var child in node.Children)
                {
                    if (child != null && child._Contains(new Vehicle { Latitude = (float)x, Longitude = (float)y }))
                        FindNearestRec(child);
                }
            }

            FindNearestRec(quadtree);
            return nearestVehicle;
        }

        public static void Main(string[] args)
        {
            var quadtree = new QuadTreeNode(-180, -90, 360, 180);
            var vehicles = new List<Vehicle>();

            // Load vehicle data from .dat file
            using (var file = new StreamReader("VehiclePositions.dat"))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    var data = line.Split('\t');
                    var vehicle = new Vehicle
                    {
                        VehicleId = int.Parse(data[0]),
                        VehicleRegistration = data[1],
                        Latitude = float.Parse(data[2]),
                        Longitude = float.Parse(data[3]),
                        RecordedTimeUTC = ulong.Parse(data[4])
                    };

                    vehicles.Add(vehicle);
                    quadtree.Insert(vehicle);
                }
            }

            // Co-ordinates to find nearest vehicle positions
            var coordinates = new List<(double Latitude, double Longitude)>
            {
                (34.544909, -102.10084),
                (32.345544, -99.123124),
                (33.234235, -100.21412),
                (35.195739, -95.348899),
                (31.895839, -97.789573),
                (32.895839, -101.78957),
                (34.115839, -100.22573),
                (32.335839, -99.992232),
                (33.535339, -94.792232),
                (32.234235, -100.22222)
            };

            foreach (var coordinate in coordinates)
            {
                var nearestVehicle = FindNearestVehicle(quadtree, coordinate.Latitude, coordinate.Longitude);
                Console.WriteLine($"Nearest vehicle to ({coordinate.Latitude}, {coordinate.Longitude}):");
                Console.WriteLine($"Vehicle ID: {nearestVehicle.VehicleId}");
                Console.WriteLine($"Vehicle Registration: {nearestVehicle.VehicleRegistration}");
                Console.WriteLine($"Latitude: {nearestVehicle.Latitude}");
                Console.WriteLine($"Longitude: {nearestVehicle.Longitude}");
                Console.WriteLine();
            }
        }
    }
}
