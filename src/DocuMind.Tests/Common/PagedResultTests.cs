using DocuMind.Application.Common;

namespace DocuMind.Tests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 20, 5)]
    public void TotalPages_RoundsUpToCoverRemainingItems(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>
        {
            Items = [],
            Page = 1,
            PageSize = pageSize,
            TotalCount = totalCount,
        };

        Assert.Equal(expectedPages, result.TotalPages);
    }
}
