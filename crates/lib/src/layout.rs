use crate::models::*;
use std::cmp::{self, Reverse};

/// Describes evaluated optimal placement of the diced units on an atlas texture.
pub(crate) struct Layout {
    /// The evaluated optimal size of the atlas texture.
    pub size: USize,
    /// Rects of the units on the atlas texture, with unit IDs.
    pub rects: Vec<(usize, URect)>,
}

/// Evaluates the smallest layout of the rects with specified sizes and unit IDs trying
/// several atlas widths, or none when the rects don't fit within the atlas size limit.
pub(crate) fn eval_layout(
    sizes: &[(usize, USize)],
    limit: u32,
    square: bool,
    pot: bool,
) -> Option<Layout> {
    let area = sizes.iter().map(|(_, s)| s.area()).sum::<u64>();
    let max_width = sizes.iter().map(|(_, s)| s.width).max().unwrap_or(0);
    let mut best: Option<Layout> = None;
    for width in candidate_widths(area, max_width, limit) {
        if let Some(rects) = place(sizes, width, limit) {
            let layout = new_layout(rects, square, pot);
            if best
                .as_ref()
                .is_none_or(|b| is_smaller(&layout.size, &b.size))
            {
                best = Some(layout);
            }
        }
    }
    best
}

/// Places all the rects into a bin of specified size; none when any of the rects doesn't fit.
fn place(sizes: &[(usize, USize)], width: u32, height: u32) -> Option<Vec<(usize, URect)>> {
    let mut sorted = sizes.to_vec();
    sorted.sort_unstable_by_key(|(id, s)| (Reverse(s.height), Reverse(s.width), *id));
    let mut free = vec![URect::new(0, 0, width, height)];
    let mut rects = Vec::with_capacity(sorted.len());
    for (id, size) in sorted {
        rects.push((id, insert_rect(&mut free, &size)?));
    }
    Some(rects)
}

/// Atlas widths to try: from 60% to 200% of the side of a square with the total area of
/// the rects, plus the max; within the min (the widest rect) and the max (the size limit).
fn candidate_widths(area: u64, min: u32, max: u32) -> Vec<u32> {
    let side = area.isqrt();
    let mut widths = [60, 70, 80, 90, 100, 110, 125, 150, 175, 200]
        .iter()
        .map(|percent| cmp::min(side * percent / 100, u32::MAX as u64) as u32)
        .chain([max])
        .filter(|&w| w >= min && w <= max)
        .collect::<Vec<_>>();
    widths.sort_unstable();
    widths.dedup();
    widths
}

/// Describes layout of the placed rects with the atlas size covering them.
fn new_layout(rects: Vec<(usize, URect)>, square: bool, pot: bool) -> Layout {
    let mut size = USize::new(0, 0);
    for (_, rect) in rects.iter() {
        size.width = cmp::max(size.width, rect.x + rect.width);
        size.height = cmp::max(size.height, rect.y + rect.height);
    }
    if pot {
        let side = cmp::max(size.width, size.height).next_power_of_two();
        size = USize::new(side, side);
    } else if square {
        let side = cmp::max(size.width, size.height);
        size = USize::new(side, side);
    }
    Layout { size, rects }
}

/// Whether the atlas size is preferable: smaller area, then closer to square, then wider.
fn is_smaller(a: &USize, b: &USize) -> bool {
    let key = |s: &USize| (s.area(), cmp::max(s.width, s.height), Reverse(s.width));
    key(a) < key(b)
}

/// Places a rect of specified size into the free rect where it ends up the highest, then
/// the leftmost (the bottom-left rule) and updates the free rects; none when it doesn't fit.
fn insert_rect(free: &mut Vec<URect>, size: &USize) -> Option<URect> {
    let mut best: Option<(u32, u32)> = None;
    for rect in free.iter() {
        if rect.width >= size.width && rect.height >= size.height {
            let key = (rect.y + size.height, rect.x);
            if best.is_none_or(|b| key < b) {
                best = Some(key);
            }
        }
    }
    let (bottom, x) = best?;
    let placed = URect::new(x, bottom - size.height, size.width, size.height);
    occupy(free, &placed);
    Some(placed)
}

/// Removes the placed rect from the free rects: the intersecting ones are split into
/// maximal pieces, of which only the ones not contained in another free rect are kept.
fn occupy(free: &mut Vec<URect>, placed: &URect) {
    let mut new = vec![];
    free.retain(|rect| {
        if rect.intersects(placed) {
            split(rect, placed, &mut new);
            false
        } else {
            true
        }
    });
    // Pieces can't contain other free rects (none is contained in another),
    // so check only the pieces for containment.
    new.sort_unstable_by_key(|r| (r.x, r.y, r.width, r.height));
    new.dedup();
    for (idx, piece) in new.iter().enumerate() {
        let in_free = free.iter().any(|r| r.contains(piece));
        let in_new = new
            .iter()
            .enumerate()
            .any(|(i, r)| i != idx && r.contains(piece));
        if !in_free && !in_new {
            free.push(*piece);
        }
    }
}

/// Splits the free rect around the placed one into up to four overlapping max pieces.
fn split(free: &URect, placed: &URect, into: &mut Vec<URect>) {
    let (free_right, free_bottom) = (free.x + free.width, free.y + free.height);
    let (placed_right, placed_bottom) = (placed.x + placed.width, placed.y + placed.height);
    if placed.x > free.x {
        into.push(URect::new(free.x, free.y, placed.x - free.x, free.height));
    }
    if placed_right < free_right {
        into.push(URect::new(
            placed_right,
            free.y,
            free_right - placed_right,
            free.height,
        ));
    }
    if placed.y > free.y {
        into.push(URect::new(free.x, free.y, free.width, placed.y - free.y));
    }
    if placed_bottom < free_bottom {
        into.push(URect::new(
            free.x,
            placed_bottom,
            free.width,
            free_bottom - placed_bottom,
        ));
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn rects_are_inserted_bottom_left() {
        let mut free = vec![URect::new(0, 0, 4, 4)];
        let rect = |x, y, w, h| Some(URect::new(x, y, w, h));
        assert_eq!(insert_rect(&mut free, &USize::new(2, 2)), rect(0, 0, 2, 2));
        assert_eq!(insert_rect(&mut free, &USize::new(2, 1)), rect(2, 0, 2, 1));
        assert_eq!(insert_rect(&mut free, &USize::new(2, 1)), rect(2, 1, 2, 1));
        assert_eq!(insert_rect(&mut free, &USize::new(4, 2)), rect(0, 2, 4, 2));
        assert_eq!(insert_rect(&mut free, &USize::new(1, 1)), None);
    }

    #[test]
    fn rects_are_placed_tallest_first() {
        let sizes = [(0, USize::new(2, 1)), (1, USize::new(1, 3))];
        let rects = place(&sizes, 4, 4).unwrap();
        assert_eq!(
            rects,
            vec![(1, URect::new(0, 0, 1, 3)), (0, URect::new(1, 0, 2, 1))]
        );
        assert!(place(&sizes, 2, 2).is_none());
    }

    #[test]
    fn layout_is_smallest_of_candidate_widths() {
        let sizes = (0..4).map(|id| (id, USize::new(1, 1))).collect::<Vec<_>>();
        let layout = eval_layout(&sizes, 4, false, false).unwrap();
        assert_eq!(layout.size, USize::new(2, 2));
        assert!(eval_layout(&sizes, 1, false, false).is_none());
    }

    #[test]
    fn layout_is_found_at_narrower_width_when_limit_width_fails() {
        let sizes = [(36, 24), (12, 16), (20, 16), (28, 36), (28, 32)]
            .iter()
            .enumerate()
            .map(|(id, &(w, h))| (id, USize::new(w, h)))
            .collect::<Vec<_>>();
        assert!(place(&sizes, 64, 64).is_none());
        let layout = eval_layout(&sizes, 64, false, false).unwrap();
        assert!(layout.size.width <= 64 && layout.size.height <= 64);
    }

    #[test]
    fn candidate_widths_are_around_square_root_of_area() {
        assert_eq!(
            candidate_widths(100, 1, 100),
            vec![6, 7, 8, 9, 10, 11, 12, 15, 17, 20, 100]
        );
        assert_eq!(candidate_widths(100, 9, 12), vec![9, 10, 11, 12]);
        assert_eq!(candidate_widths(100, 50, 40), Vec::<u32>::new());
    }
}
