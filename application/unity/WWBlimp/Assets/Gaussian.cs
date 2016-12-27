using System;

// adapted from http://stackoverflow.com/questions/1303368/how-to-generate-normally-distributed-random-from-an-integer-range#1303406

public class Gaussian 
{
	 double next_gaussian = 0.0;
	 Random random = new Random();
	bool uselast = true;

	double min;
	double max;
	double deviation;
	double offset;

	//  will generate a standard nomral distribution between min and max
	//  will cut off the .3 percent outside min and max and recalc

	public Gaussian(double min, double max) {
		this.min = min;
		this.max = max;

		double halfDiff = (max - min) / 2.0;
		offset = min + halfDiff;
		deviation = halfDiff / 3.0; // this is a "standard" deviation
	}


	public double BoxMuller()
	{
		if (uselast) 
		{ 
			uselast = false;
			return next_gaussian;
		}
		else
		{
			double v1, v2, s;
			do
			{
				v1 = 2.0 * random.NextDouble() - 1.0;
				v2 = 2.0 * random.NextDouble() - 1.0;
				s = v1 * v1 + v2 * v2;
			} while (s >= 1.0 || s == 0);

			s = System.Math.Sqrt((-2.0 * System.Math.Log(s)) / s);

			next_gaussian = v2 * s;
			uselast = true;
			return v1 * s;
		}
	}

	public double BoxMuller(double mean, double standard_deviation)
	{
		return mean + BoxMuller() * standard_deviation;
	}

	public double next() {
		double result = BoxMuller(offset, deviation);
		while((result < min) || (result > max)) {
			// pick new value if out of range
			 result = BoxMuller(offset, deviation);
		}
		return result;
	}
}