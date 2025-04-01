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
                    return '$' + parseFloat(data).toFixed(2);
                }
            },
            { "data": "categoryName" },
            { "data": "brandName" },
            { "data": "productTypeName" },
            { "data": "stockQuantity" },
            {
                "data": "isBestseller",
                "render": function (data, type, row) {
                    return data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-secondary">No</span>';
                }
            },
            {
                "data": "productID",
                "render": function (data) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Product/Ubsert/${data}" class="btn btn-primary btn-sm">
                                <i class="bi bi-pencil-square"></i> Edit
                            </a>
                            <button onclick="Delete(${data})" class="btn btn-danger btn-sm">
                                <i class="bi bi-trash-fill"></i> Delete
                            </button>
                        </div>
                    `;
                }
            }
        ],
        "responsive": true,
        "autoWidth": false,
        "language": {
            "emptyTable": "No products found."
        },
        "columnDefs": [ // Move actions column to the end
            {
                "targets": -1,
                "orderable": false // Disable sorting for the actions column
            }
        ]
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