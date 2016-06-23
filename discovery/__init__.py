from discovery import Discovery

__all__ = ['Discovery']

import sys as _sys

try:
    from discovery import *
except ImportError:
    del _sys.modules[__name__]
    raise