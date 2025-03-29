$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    $('#myTable').DataTable({
        "ajax": {
            "url": "/Product/GetAll",
            "type": "GET",
            "dataType": "json"
        },
        "columns": [
            { "data": "name" },
            { "data": "description" },
            {
                "data": "price",
                "render": function (data) {
                    return '$' + data.toFixed(2);
                }
            },
            { "data": "categoryName" },
            { "data": "brandName" },
            { "data": "productTypeName" },
            { "data": "stockQuantity" },
            {
                "data": "productID",
                "render": function (data) {
                    return `
                        <div class="btn-group">
                            <a href="/Product/Ubsert/${data}" class="btn btn-primary">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                            <button onclick="Delete(${data})" class="btn btn-danger">
                                <i class="bi bi-trash-fill"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        "responsive": true,
        "autoWidth": false
    });
}

function Delete(id) {
    Swal.fire({
        title: 'Confirm Delete?',
        text: "This action cannot be undone!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/Product/Delete/${id}`,
                type: 'DELETE',
                success: function (response) {
                    toastr.success(response.message);
                    $('#myTable').DataTable().ajax.reload();
                },
                error: function (xhr) {
                    toastr.error(xhr.responseJSON.message);
                }
            });
        }
    });
}