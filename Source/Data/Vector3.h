#pragma once

class Vector3
{
public:
	double x;
	double y;
	double z;

	Vector3();
	Vector3(const Vector3 &);
	Vector3(double x, double y, double z);

	Vector3 operator+(const Vector3 &) const;
	Vector3 operator*(double) const;
	Vector3 operator/(double) const;
};