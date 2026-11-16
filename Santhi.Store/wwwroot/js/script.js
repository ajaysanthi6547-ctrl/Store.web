
// Shopping Cart
let cartCount = 0;

function addToCart() {
    cartCount++;

    document.getElementById("cartCount").textContent =
        cartCount;

    alert("Product added to your cart!");
}

// Product Search
function searchProducts() {

    let input = document
        .getElementById("searchInput")
        .value.toLowerCase();

    let products = document.querySelectorAll(".product-card");

    products.forEach(function(product) {

        let name = product
            .querySelector("h3")
            .textContent.toLowerCase();

        if (name.includes(input)) {
            product.style.display = "block";
        } else {
            product.style.display = "none";
        }

    });
}
