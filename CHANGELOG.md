# Infinite Dynamic Scroll View Unity changelog

## [2.0.1] - 2026-02-08

### Fixed
- Cells at top/bottom might be created and destroyed again each frame.
- Top padding is ignored.

## [2.0.0] - 2026-01-30

### Added
 - ScrollTo(index) method to scroll to a specific cell
 - Exposed stored Data as IReadOnlyList<TData>

### Changed
 - **BREAKING:** ScrollPosition cannot be set from code anymore (use ScrollTo instead).

### Fixed
- Calling ScrollView.Clear() might not work in every case.

## [1.2.0] - 2026-01-26

### Added
- Option to get visible cells
- OnValueChanged event, called when the scroll position changes
- Feature to add items also to the top/front of the ScrollView

### Changed
- Improved performance by removing unnecessary update calls

### Fixed
- possible error "Trying to remove XXX from rebuild list while we are already inside a rebuild loop." on RectTransform changes
- error is thrown when ScrollPosition is set to zero and no old cell is visible anymore

## [1.1.0] - 2026-01-22

### Added
- New example using multiple prefabs and custom object pooling
- ScrollPosition can be set from code
- Support for height changes of the ScrollView

### Changed
- Renamed example folders

### Fixed
- Removed superfluous ContentSizeFitter from the example’s SimpleScrollCell prefab
- Error on missing .meta file for LICENSE
- Some anchor values might break the view

## [1.0.0] - 2026-01-12

### Added
- initial release~~~~
