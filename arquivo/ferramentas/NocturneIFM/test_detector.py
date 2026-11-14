import unittest
import numpy as np
from nocturne_ifm import candidates


class DetectorTests(unittest.TestCase):
    def frame(self, fill):
        a = np.full((120, 220, 3), 140, dtype=np.uint8)
        a[40:46, 30:130] = 25
        a[40:46, 30:30+fill] = [200, 35, 35]
        return a

    def test_low_bar(self):
        hits = candidates(self.frame(20), 100, .3)
        self.assertTrue(hits)
        self.assertAlmostEqual(hits[0][2], .2)

    def test_healthy_bar(self):
        self.assertFalse(candidates(self.frame(80), 100, .3))

    def test_no_bar(self):
        self.assertFalse(candidates(np.zeros((120, 220, 3), dtype=np.uint8), 100, .3))

    def test_red_without_empty_background(self):
        a = self.frame(20)
        a[40:46, 50:130] = 140
        self.assertFalse(candidates(a, 100, .3))


if __name__ == '__main__':
    unittest.main()
