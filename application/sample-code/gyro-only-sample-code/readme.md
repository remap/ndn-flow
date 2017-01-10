Gyroscope test code. 

Followed connection guide and sketch here: http://www.rfduino.com/product/rfduino-6-axis-mpu-6050-accgyro-demo/

(Unlike what the video demo seems to suggest, we do have VCC connected to SDA and SCL with 10K resistor in between)

Seem to need to hold for a while for readings to stabilize. Need to hold firmly for now (all 5 wires connect) or reading stops, or device couldn't be successfully recognized in the first place.

Added arbitrary Quaternion to Euler, data reading seems to begin to make sense.