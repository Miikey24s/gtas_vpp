from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(r"D:\WORK\gtas_vpp")
AUDIT = ROOT / "audit" / "runs" / "20260528T040959Z" / "screenshots"
OUTPUT = ROOT / "LVTN" / "screenshots" / "ch03"
OUTPUT.mkdir(parents=True, exist_ok=True)


SCREENSHOTS = [
    (ROOT / "tmp" / "03d-login-current.png", "ui-login.png", None),
    (AUDIT / "dashboard.my-orders-desktop.png", "ui-dashboard-my-orders.png", (0, 0, 1920, 1080)),
    (AUDIT / "dashboard.history-desktop.png", "ui-order-history.png", (0, 0, 1920, 540)),
    (AUDIT / "dashboard.order-create.new-desktop.png", "ui-order-create.png", (0, 0, 1920, 1080)),
    (AUDIT / "dashboard.period-operations-desktop.png", "ui-period-operations.png", (0, 0, 1920, 660)),
    (AUDIT / "library.pricing.price-lists-desktop.png", "ui-price-lists.png", (0, 0, 1920, 380)),
    (AUDIT / "library.items-desktop.png", "ui-library-items.png", (0, 0, 1920, 1080)),
    (AUDIT / "permission.component-desktop.png", "ui-permission-groups.png", (0, 0, 1920, 380)),
    (AUDIT / "dashboard.management.all-desktop.png", "ui-all-orders-summary.png", (0, 0, 1920, 540)),
]


for source, filename, crop_box in SCREENSHOTS:
    if not source.exists():
        raise FileNotFoundError(source)
    image = ImageOps.exif_transpose(Image.open(source)).convert("RGB")
    if crop_box is not None:
        right = min(crop_box[2], image.width)
        bottom = min(crop_box[3], image.height)
        image = image.crop((crop_box[0], crop_box[1], right, bottom))
    image = ImageOps.expand(image, border=2, fill=(150, 150, 150))
    destination = OUTPUT / filename
    image.save(destination, format="PNG", optimize=True)
    print(f"{filename}: {image.width}x{image.height}")

