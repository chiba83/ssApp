using AutoMapper;
using ssAppModels.EFModels;

namespace ssAppBlazorWeb.Extensions;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    // 継承元プロパティもすべてマッピング
    CreateMap<DailyOrderNews, Order>()
      .ForMember(dest => dest.GroupKey, opt => opt.MapFrom((x, _) =>
        $"{x.LastOrderDate:yy/MM/dd - HH:mm:ss} / {x.ShopCode} / " +
        $"{x.PackingId?.Split('-').Last() ?? ""} / {x.ShipPrefecture} / {x.ShipName}"))
      .ForMember(dest => dest.OrderDateForGrid, opt => opt.MapFrom(x =>
        x.OrderDate.ToString("yy/MM/dd - HH:mm:ss")))
      .ForMember(dest => dest.OrderIdForGrid, opt => opt.MapFrom((x, _) =>
        x.OrderId.Split('-').Last()))
      .ForMember(dest => dest.MultiOrders, opt => opt.Ignore());
  }
}
