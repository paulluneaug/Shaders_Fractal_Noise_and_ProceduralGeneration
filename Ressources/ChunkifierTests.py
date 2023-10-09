
def CoorToVals(x : int, y : int) -> int:
    scale = 1
    
def NextPowerOfTwo(n : int):
    shift = 0
    a = False
    isPowerOfTwo = n > 0
    while (n != 0):
        if (a):
            isPowerOfTwo = False
            a = False
        if (n & 1 == 1):
            a = True
        
        shift += 1
        n = n >> 1
    return shift - isPowerOfTwo

def TestSmallestGreaterPowerOfTwo(n):
    print(f"{n} : {1 << NextPowerOfTwo(n)}")
        
    
def LocalOffsetToLocalCoordinates(offset : int) -> tuple:
    x = 0
    y = 0
    z = 0
    
    pow = 0
    while ((1 << pow) <= offset):
    
        coordInDim = OffsetToCoordinatesInCubeOfDim(offset, pow)
        x += coordInDim[0] * (1 << (pow - 1))
        y += coordInDim[1] * (1 << (pow - 1))
        z += coordInDim[2] * (1 << (pow - 1))
        pow += 3
    
    return (x, y, z)


def OffsetToCoordinatesInCubeOfDim(offset, dim):

    localOffset = offset % (1 << (dim * 3))
    return (localOffset % (1 << 0), localOffset % (1 << 1), localOffset % (1 << 2))
    
def LocalOffsetToLocalCoordinates2D(offset : int) -> tuple:
    x = 0
    y = 0
    rst = offset
    
    pow = NextPowerOfTwo(offset)
    while (pow > 0):
        coordInDim = OffsetToCoordinatesInSquareOfDim(rst, pow)
        print(coordInDim)
        shift = max(0, (pow))
        x += coordInDim[0] * pow
        y += coordInDim[1] * pow
        rst = coordInDim[2]
        pow -= 1
    
    return (x, y)


def OffsetToCoordinatesInSquareOfDim(offset, pow):

    dim = (1 << ((2 * pow)))
    localOffset = offset % dim
    print(f"{offset} <=> {localOffset} in dim {dim} (pow = {pow})")
    return (localOffset % (1 << pow), localOffset // (1 << pow), offset % dim)

size = 4
l = [[0 for _ in range(size)] for _ in range(size)]

for i in range(size * size):
    coords = LocalOffsetToLocalCoordinates2D(i)
    print(coords, i)
    print("\n")
    # l[coords[1]][coords[0]] = i
    
for il in range(size):
    print(l[il])

print(OffsetToCoordinatesInSquareOfDim(11, 2))
TestSmallestGreaterPowerOfTwo(0)
TestSmallestGreaterPowerOfTwo(1)
TestSmallestGreaterPowerOfTwo(2)
TestSmallestGreaterPowerOfTwo(3)
TestSmallestGreaterPowerOfTwo(7)
TestSmallestGreaterPowerOfTwo(8)
TestSmallestGreaterPowerOfTwo(67)


#  0  1  4  5 
#  2  3  6  7
#  8  9 12 13
# 10 11 14 15